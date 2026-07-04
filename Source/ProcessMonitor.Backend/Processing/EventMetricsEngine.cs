using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;

using ProcessMonitor.Backend.Models;

using ProcessMonitor.Shared.Snapshots;

namespace ProcessMonitor.Backend.Processing;

public sealed class EventMetricsEngine 
{
    private ChannelReader<RawKernelEvent> _input;
    private ChannelWriter<ProcessMetricsSnapshot> _output;
    
    public EventMetricsEngine(
        Channel<RawKernelEvent> input,
        Channel<ProcessMetricsSnapshot> output)
    {
        _input = input.Reader;
        _output = output.Writer;
    }

    // Note: Consier bringing interface similar to Cancellation layer
    // for future testing and consistent hosting with other classes
    public async Task RunAsync(CancellationToken ct)
    {
        // TODO: Unhardcode snapshot initialization
        // TODO: Properly handle WaitToReadAsync exceptions
        // TODO: Add logging when cancellation is requested
        // TODO: Add timeQuantum delay support
        while (await _input.WaitToReadAsync(ct))
        {
            var inputEvent = await _input.ReadAsync(ct);
            
            var snapshot = new ProcessMetricsSnapshot()
            {
                ProcessId = inputEvent.ProcessId,
                ProcessName = "NOT IMPLEMENTED",
                CpuUsage = 1234.567,
                ThreadCount = 1,
                TimestampUtc = DateTime.Now,
                Threads = new List<ThreadMetricsSnapshot>()
            };

            await _output.WriteAsync(snapshot);
        }
    }
}
