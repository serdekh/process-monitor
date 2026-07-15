using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Tracing;

using ProcessMonitor.Backend.State;
using ProcessMonitor.Shared.Snapshots;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace ProcessMonitor.Backend.Processing;

public class ProcessRuntimeState
{
    public ConcurrentDictionary<int, double> ThreadsExecutionStarts { get; set; } = new();

    public double TotalCpuTimeMs = 0;
}

public sealed class EventMetricsEngine 
{
    private readonly ChannelReader<TraceEvent> _input;
    private readonly ChannelWriter<ProcessMetricsSnapshot> _output;
    private readonly ILogger<EventMetricsEngine> _logger;
    private readonly MonitoringSessionState _state;
    
    private ProcessRuntimeState _processRuntimeState;
    
    public EventMetricsEngine(
        Channel<TraceEvent> input,
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
            if (_processRuntimeState.ThreadsExecutionStarts.TryRemove(data.OldThreadID, out double startTime))
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
            _processRuntimeState.ThreadsExecutionStarts[data.NewThreadID] = data.TimeStampRelativeMSec;
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
                while (_input.TryRead(out var inputEvent))
                {
                    if (inputEvent is null) continue;

                    if (inputEvent is CSwitchTraceData cswitchData)
                    {
                        ComputeCpuUsage(cswitchData);
                    }

                    snapshot.ProcessName = inputEvent.ProcessName;
                    snapshot.TimestampUtc = DateTime.UtcNow;

                    if (inputEvent.EventName == "Thread/Start")
                    {
                        snapshot.ThreadCount++;
                    }
                    else if (inputEvent.EventName == "Thread/Stop")
                    {
                        snapshot.ThreadCount--;
                    }
                }
            }
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("[Processing]: Could not compute metrics: cancellation requested.");
            return;
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
                snapshot.CpuUsage = (_processRuntimeState.TotalCpuTimeMs / elapsedMs) * 100;
            }
            System.Console.WriteLine($"Accumulated CPU Time: {_processRuntimeState.TotalCpuTimeMs:F2} ms over {elapsedMs:F2} ms window");
            

            await _output.WriteAsync(snapshot, ct);
            
            _processRuntimeState.TotalCpuTimeMs = 0; 
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("[Processing]: Could not write the computed metrics (interrupted).");
            return;
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
