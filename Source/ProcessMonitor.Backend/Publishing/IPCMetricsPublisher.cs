using System;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Transport;
using ProcessMonitor.Shared.Serialization;

namespace ProcessMonitor.Backend.Publishing;

// TODO: Add logging
public sealed class IPCMetricsPublisher : IMetricsPublisher 
{
    private readonly IMessageSerializer _serializer;
    private readonly ITelemetryTransport _transport;

    public IPCMetricsPublisher(IMessageSerializer serializer, ITelemetryTransport transport)
    {
        _serializer = serializer;
        _transport = transport;
    }

    public async Task PublishAsync(ProcessMetricsSnapshot snapshot, CancellationToken ct)
    {
        if (!ct.IsCancellationRequested)
        {
            var bytes = _serializer.Serialize(snapshot);

            await _transport.SendAsync(bytes, ct);
        }
    }
}
