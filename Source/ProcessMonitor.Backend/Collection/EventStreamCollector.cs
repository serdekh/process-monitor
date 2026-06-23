using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using ProcessMonitor.Backend.Models;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventStreamCollector : IEventCollector
{
    private ChannelWriter<RawKernelEvent> _writer;

    public EventStreamCollector(Channel<RawKernelEvent> input)
    { 
        _writer = input.Writer;
    }

    // TODO: Replace hardcoded raw kernel event creation with
    // real ETW session that subscribes to kernel provider
    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await _writer.WriteAsync(new RawKernelEvent
            {
                ProcessId = 1234,
                EventName = "Temp",
                Timestamp = DateTime.UtcNow 
            });

            await Task.Delay(1000, ct);
        }

        // TODO: log exiting when cancellation is requested.
    }
}
