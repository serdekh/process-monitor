using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.State;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventStreamCollector(
    Channel<RawEvent> input,
    ILogger<EventStreamCollector> logger,
    IHostApplicationLifetime hostLifetime,
    MonitoringSessionState state) : IEventCollector
{
    public static string SessionName => "ProcessMonitor.Backend.TraceEventSession";

    private readonly ChannelWriter<RawEvent> _writer = input.Writer;
    private readonly ILogger<EventStreamCollector> _logger = logger;
    private readonly IHostApplicationLifetime _hostLifetime = hostLifetime;
    private readonly MonitoringSessionState _state = state;

    private readonly HashSet<int> _targetThreadIds = [];

    private int _targetProcessId = -1;

    private bool HasTargetProcess => _targetProcessId > 0;

    private bool TryUpdateTargetProcess()
    {
        var processId = _state.ProcessId ?? -1;

        if (processId == _targetProcessId) return false;

        _targetProcessId = processId;
        _targetThreadIds.Clear();

        if (_targetProcessId <= 0)
        {
            _logger.LogInformation("[Collection]: No target process configured.");

            return true;
        }

        SeedExistingThreads(_targetProcessId);

        _logger.LogInformation("[Collection]: Target process changed to PID {ProcessId}.", _targetProcessId);

        return true;
    }

    private void SeedExistingThreads(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            foreach (ProcessThread thread in process.Threads)
                _targetThreadIds.Add(thread.Id);

            _logger.LogDebug("[Collection]: Seeded {ThreadCount} existing threads for PID {ProcessId}.",
                _targetThreadIds.Count,
                processId);
        }
        catch (ArgumentException)
        {
            _logger.LogDebug(
                "[Collection]: Process {ProcessId} does not exist.", processId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "[Collection]: Could not enumerate threads for PID {ProcessId}.", processId);
        }
    }

    private bool IsTargetProcessEvent(TraceEvent data)
    {
        return HasTargetProcess && data.ProcessID == _targetProcessId;
    }

    private bool IsTargetContextSwitch(CSwitchTraceData data)
    {
        return HasTargetProcess &&
            (_targetThreadIds.Contains(data.OldThreadID) ||
            _targetThreadIds.Contains(data.NewThreadID));
    }

    private bool TryWriteEvent(TraceEvent data, RawEventKind kind)
    {
        try
        {
            var rawEvent = new RawEvent(data.Clone(), kind);

            if (_writer.TryWrite(rawEvent)) return true;

            _logger.LogWarning("[Collection]: Input channel rejected {EventKind} event.", kind.AsString());

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Collection]: Failed to enqueue {EventKind} event.", kind.AsString());

            _hostLifetime.StopApplication();

            return false;
        }
    }

    private void HandleThreadStart(ThreadTraceData data)
    {
        if (!IsTargetProcessEvent(data)) return;

        _targetThreadIds.Add(data.ThreadID);

        TryWriteEvent(data, RawEventKind.ThreadStart);
    }

    private void HandleThreadDCStart(ThreadTraceData data)
    {
        if (!IsTargetProcessEvent(data)) return;

        _targetThreadIds.Add(data.ThreadID);

        TryWriteEvent(data, RawEventKind.ThreadDCStart);
    }

    private void HandleThreadStop(ThreadTraceData data)
    {
        if (!_targetThreadIds.Contains(data.ThreadID)) return;

        TryWriteEvent(data, RawEventKind.ThreadStop);

        _targetThreadIds.Remove(data.ThreadID);
    }

    private void HandleThreadDCEnd(ThreadTraceData data)
    {
        if (!_targetThreadIds.Contains(data.ThreadID)) return;

        TryWriteEvent(data, RawEventKind.ThreadDCEnd);

        _targetThreadIds.Remove(data.ThreadID);
    }

    private void HandleContextSwitch(CSwitchTraceData data)
    {
        if (!IsTargetContextSwitch(data)) return;

        TryWriteEvent(data, RawEventKind.ContextSwitch);
    }

    private void HandleSyscall(TraceEvent data, RawEventKind kind)
    {
        if (!IsTargetProcessEvent(data)) return;

        TryWriteEvent(data, kind);
    }

    private void HandleEvent(TraceEvent data)
    {
        TryUpdateTargetProcess();

        if (!HasTargetProcess) return;

        var kind = data.ToRawEventKind();

        switch (kind)
        {
            case RawEventKind.ThreadStart:
                if (data is ThreadTraceData threadStart) HandleThreadStart(threadStart);
                break;

            case RawEventKind.ThreadDCStart:
                if (data is ThreadTraceData threadDcStart) HandleThreadDCStart(threadDcStart);
                break;

            case RawEventKind.ThreadStop:
                if (data is ThreadTraceData threadStop) HandleThreadStop(threadStop);
                break;

            case RawEventKind.ThreadDCEnd:
                if (data is ThreadTraceData threadDcEnd) HandleThreadDCEnd(threadDcEnd);
                break;

            case RawEventKind.ContextSwitch:
                if (data is CSwitchTraceData contextSwitch) HandleContextSwitch(contextSwitch);
                break;

            case RawEventKind.SyscallEnter:
                HandleSyscall(data, RawEventKind.SyscallEnter);
                break;

            case RawEventKind.SyscallExit:
                break;

            case RawEventKind.Undefined:
            
            default: break;
        }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        using var oldSession = new TraceEventSession(SessionName);

        _logger.LogDebug("[Collection]: Stopping previously created {SessionName} session.", SessionName);

        oldSession.Stop();

        if (TraceEventSession.IsElevated() != true)
        {
            _logger.LogError("[Collection]: Could only run as administrator.");

            _hostLifetime.StopApplication();

            return;
        }

        using var session = new TraceEventSession(SessionName);

        var kernelKeywords =
              KernelTraceEventParser.Keywords.Process
            | KernelTraceEventParser.Keywords.Thread
            | KernelTraceEventParser.Keywords.ContextSwitch
            | KernelTraceEventParser.Keywords.SystemCall;

        session.EnableKernelProvider(kernelKeywords);

        session.Source.Kernel.All += HandleEvent;

        _logger.LogInformation("[Collection]: Event collection started.");

        var processingTask = Task.Run(
            () => session.Source.Process(),
            CancellationToken.None);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
        }
        finally
        {
            session.Stop();

            try
            {
                await processingTask;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[Collection]: TraceEvent processing terminated.");
            }

            _writer.TryComplete();

            _logger.LogInformation("[Collection]: Terminated.");
        }
    }
}