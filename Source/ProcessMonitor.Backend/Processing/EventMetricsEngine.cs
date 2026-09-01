using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.State;
using ProcessMonitor.Shared.Snapshots;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using ProcessMonitor.Backend.Models;
using System.Collections.Generic;

namespace ProcessMonitor.Backend.Processing;

public class ProcessRuntimeState
{
    public HashSet<int> ThreadIds { get; set; } = [];

    public Dictionary<int, double> ThreadExecutionStarts { get; set; } = [];

    public double TotalCpuTimeMs { get; set; } 
}

public sealed class EventMetricsEngine 
{
    private readonly ChannelReader<RawEvent> _input;
    private readonly ChannelWriter<ProcessMetricsSnapshot> _output;
    private readonly ILogger<EventMetricsEngine> _logger;
    private readonly MonitoringSessionState _state;
    
    private ProcessRuntimeState _processRuntimeState;
    
    public EventMetricsEngine(
        Channel<RawEvent> input,
        Channel<ProcessMetricsSnapshot> output,
        ILogger<EventMetricsEngine> logger,
        MonitoringSessionState state)
    {
        _input = input.Reader;
        _output = output.Writer;
        _logger = logger;
        _state = state;

        _processRuntimeState = new();
    }

    private void ComputeCpuUsage(CSwitchTraceData data)
    {
        if (data.OldProcessID == _state.ProcessId)
        {     
            if (_processRuntimeState.ThreadExecutionStarts.Remove(data.OldThreadID, out double startTime))
            {
                double threadTimeDurationMs = data.TimeStampRelativeMSec - startTime;

                if (threadTimeDurationMs > 0)
                {
                    _processRuntimeState.TotalCpuTimeMs += threadTimeDurationMs;
                }
            }
        }
        
        if (data.NewProcessID == _state.ProcessId)
        {
            _processRuntimeState.ThreadExecutionStarts[data.NewThreadID] = data.TimeStampRelativeMSec;
        }
    }

    private void HandleRawEvent(ref ProcessMetricsSnapshot acc, RawEvent rawEvent)
    {
        if (rawEvent.Source is null) return;

        switch (rawEvent.Kind)
        {
            case RawEventKind.ContextSwitch:
                acc.ContextSwitchesCount++;
                ComputeCpuUsage((CSwitchTraceData)rawEvent.Source); break;
            case RawEventKind.ThreadStart:
                _processRuntimeState.ThreadIds.Add(rawEvent.Source.ThreadID); break;
            case RawEventKind.ThreadStop:
                _processRuntimeState.ThreadIds.Remove(rawEvent.Source.ThreadID); break;
            case RawEventKind.SyscallEnter:
                acc.SyscallsCount++; break;

            default: return;
        }

        if (acc.ProcessId == rawEvent.Source.ProcessID)
        {
            acc.ProcessName = rawEvent.Source.ProcessName;
            acc.TimestampUtc = DateTime.UtcNow;
        }
    }

    private async Task ProcessFor(TimeSpan delay, CancellationToken ct)
    {
        var processId = _state.ProcessId;
        if (processId is null) return;

        var snapshot = new ProcessMetricsSnapshot
        {
            ProcessId = (int)processId
        };

        using var timeoutCts = new CancellationTokenSource(delay);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
        var linkedCt = linkedCts.Token;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            while (await _input.WaitToReadAsync(linkedCt))
            {
                while (_input.TryRead(out var rawEvent))
                {
                    HandleRawEvent(ref snapshot, rawEvent);
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError("[Processing]: Unexpected error reading incoming event: {}", ex.Message);
            return;    
        }

        try
        {
            stopwatch.Stop();

            double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            
            if (elapsedMs > 0)
            {
                snapshot.CpuUsage = _processRuntimeState.TotalCpuTimeMs / elapsedMs * 100;
            }

            snapshot.ThreadCount = _processRuntimeState.ThreadIds.Count;

            Console.WriteLine(snapshot.ToString());
            
            await _output.WriteAsync(snapshot, ct);
            
            _processRuntimeState.TotalCpuTimeMs = 0; 
        }
        catch (Exception ex)
        {
            _logger.LogError("[Processing]: Could not write the computed metrics: {}", ex.Message);
        }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogError("[Processing]: Could not start processing: cancellation requested.");
            return;    
        }

        while (!ct.IsCancellationRequested)
        {
            await ProcessFor(TimeSpan.FromMilliseconds(400), ct);
        }
    }
}
