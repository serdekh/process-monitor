using System;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.Transport;

using ProcessMonitor.Contracts.Snapshots;
using ProcessMonitor.Shared.Serialization;

namespace ProcessMonitor.Backend.Publishing;

public sealed class IPCMetricsPublisher : IMetricsPublisher 
{
    private IMessageSerializer _serializer;
    private ITelemetryTransport _transport;

    public IPCMetricsPublisher(IMessageSerializer serializer, ITelemetryTransport transport)
    {
        _serializer = serializer;
        _transport = transport;
    }

    public async Task PublishAsync(ProcessMetricsSnapshot snapshot, CancellationToken ct)
    {
        if (!ct.IsCancellationRequested)
        {
            var bytes = _serializer.Serialize<ProcessMetricsSnapshot>(snapshot);
            
            Console.WriteLine(bytes);

            await _transport.SendAsync(bytes, ct);
        }
        else
        {
            Console.WriteLine("Info: Publishing is cancelled");
        }
    }
}
