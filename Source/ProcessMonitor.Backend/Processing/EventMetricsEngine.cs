using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;

using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Tracing;

using ProcessMonitor.Shared.Snapshots;

namespace ProcessMonitor.Backend.Processing;

public sealed class EventMetricsEngine 
{
    private readonly ChannelReader<TraceEvent> _input;
    private readonly ChannelWriter<ProcessMetricsSnapshot> _output;
    private readonly ILogger<EventMetricsEngine> _logger;
    
    public EventMetricsEngine(
        Channel<TraceEvent> input,
        Channel<ProcessMetricsSnapshot> output,
        ILogger<EventMetricsEngine> logger)
    {
        _input = input.Reader;
        _output = output.Writer;
        _logger = logger;
    }

    // Note: Consider bringing interface similar to Cancellation layer
    // for future testing and consistent hosting with other classes
    public async Task RunAsync(CancellationToken ct)
    {
        // TODO: Unhardcode snapshot initialization
        // TODO: Add timeQuantum delay support
        if (ct.IsCancellationRequested)
        {
            _logger.LogError("[Processing]: Could not start processing: cancellation requested.");
            return;    
        }

        while (!ct.IsCancellationRequested)
        {
            TraceEvent inputEvent;

            try
            {
                inputEvent = await _input.ReadAsync(ct);
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("[Processing]: Could not read the incoming event: cancellation requested.");
                return;    
            }

            var snapshot = new ProcessMetricsSnapshot()
            {
                ProcessId = inputEvent.ProcessID,
                ProcessName = "NOT IMPLEMENTED",
                CpuUsage = 1234.567,
                TimestampUtc = DateTime.Now,
                Threads = new List<ThreadMetricsSnapshot>()
            };

            if (inputEvent.EventName == "Thread/Start")
            {
                snapshot.ThreadCount++;
            }
            else if (inputEvent.EventName == "Thread/Stop")
            {
                snapshot.ThreadCount--;
            }

            await _output.WriteAsync(snapshot, ct);
        }
    }
}
