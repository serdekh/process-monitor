using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

using ProcessMonitor.Backend.State;
using ProcessMonitor.Backend.Models;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using System.Collections.Generic;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventStreamCollector(
    Channel<RawEvent> input,
    ILogger<EventStreamCollector> logger,
    IHostApplicationLifetime hostLifetime,
    MonitoringSessionState state) : IEventCollector
{
    // NOTE: Consider adding a configuration manager in the future that handles
    // loading a custom session name
    public static string SessionName { get; } = "ProcessMonitor.Backend.TraceEventSession";

    private readonly ChannelWriter<RawEvent> _writer = input.Writer;

    private readonly ILogger<EventStreamCollector> _logger = logger;

    private readonly IHostApplicationLifetime _hostLifetime = hostLifetime;

    private readonly MonitoringSessionState _state = state;

    private readonly Dictionary<int, (int ProcessId, int ThreadId)> _runningThreads = [];

    private bool TryInitializeCollection()
    {
        using var oldSession = new TraceEventSession(SessionName);

        _logger.LogDebug("[Collection]: (Init) Stopping previously created {sessionName} session", SessionName);

        oldSession.Stop();

        if (TraceEventSession.IsElevated() != true)
        {
            _logger.LogError("[Collection]: Could only run as administrator.");
            _hostLifetime.StopApplication();
            return false;
        }

        return true;
    }

    private bool IsWriteable(int processId, TraceEvent e)
    {
        return e switch
        {
            CSwitchTraceData cs =>
                cs.OldProcessID == processId ||
                cs.NewProcessID == processId,

            ThreadTraceData thread =>
                thread.ProcessID == processId,

            _ =>
                e.ProcessID == processId
        };
    }

    private void TryWriteEvent(TraceEvent e, RawEventKind kind)
    {
        var processId = _state.ProcessId;

        if (processId is null) return;

        if (!IsWriteable((int)processId, e)) return;

        try
        {
            _writer.TryWrite(new RawEvent(e, kind));
        } 
        catch (Exception)
        {
            _hostLifetime.StopApplication();
        }
    }

    private void TryWriteContextSwitchEvent(CSwitchTraceData data)
    {
        _runningThreads[data.ProcessorNumber] = (data.NewProcessID, data.NewThreadID);
        TryWriteEvent(data, RawEventKind.ContextSwitch);
    }

    private void TryWriteThreadStartEvent(ThreadTraceData data)
    {
        if (data.ProcessID != _state.ProcessId) return;

        _writer.TryWrite(new RawEvent(data, RawEventKind.ThreadStart));
    }

    private void TryWriteThreadStopEvent(ThreadTraceData data)
    {
        if (data.ProcessID != _state.ProcessId) return;

        _writer.TryWrite(new RawEvent(data, RawEventKind.ThreadStop));
    }

    private void TryWriteSyscallEnterEvent(SysCallEnterTraceData data)
    {
        if (!_runningThreads.TryGetValue(data.ProcessorNumber, out var running)) return;
        
        if (running.ProcessId != _state.ProcessId) return;
    
        _writer.TryWrite(new RawEvent(data, RawEventKind.SyscallEnter));
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (!TryInitializeCollection()) return;

        using var session = new TraceEventSession(SessionName);

        var kernelKeywords = KernelTraceEventParser.Keywords.Process 
            | KernelTraceEventParser.Keywords.Thread 
            | KernelTraceEventParser.Keywords.ContextSwitch
            | KernelTraceEventParser.Keywords.SystemCall;
        
        session.EnableKernelProvider(kernelKeywords);

        session.Source.Kernel.ThreadStart += data => TryWriteThreadStartEvent(data);
        session.Source.Kernel.ThreadStop += data => TryWriteThreadStopEvent(data);

        session.Source.Kernel.ThreadCSwitch += data => TryWriteContextSwitchEvent(data);

        session.Source.Kernel.PerfInfoSysClEnter += data => TryWriteSyscallEnterEvent(data);

        var collecting = Task.Run(() => 
        {
            _logger.LogInformation("[Collection]: Starting...");
            session.Source.Process();
        }, ct);

        try 
        {
            await Task.Delay(Timeout.Infinite, ct);
        }      
        catch (Exception ex)
        {
            _logger.LogError("[Collection]: Could not read input events: {}. Terminating...", ex.Message);
        }

        session.Stop();

        await collecting;

        _logger.LogInformation("[Collection]: Terminated.");
    }
}
