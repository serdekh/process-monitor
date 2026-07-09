using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Transport;
using ProcessMonitor.Shared.Serialization;
using System.Linq;
using ProcessMonitor.Shared.Protocol;

namespace ProcessMonitor.Backend.Publishing;

public sealed class IPCMetricsPublisher : IMetricsPublisher, IDisposable
{
    private readonly IMessageSerializer _serializer;
    private readonly ITelemetryTransport _transport;

    private readonly ILogger<IPCMetricsPublisher> _logger;

    public IPCMetricsPublisher(
        IMessageSerializer serializer, 
        ITelemetryTransport transport,
        ILogger<IPCMetricsPublisher> logger)
    {
        _serializer = serializer;
        _transport = transport;
        _logger = logger;
    }

    public void Dispose()
    {
        Deinitialize();
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await _transport.InitializeAsync(ct);
    }

    public void Deinitialize()
    {
        _transport.Deinitialize();
    }

    public async Task PublishAsync(ProcessMetricsSnapshot snapshot, CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {   
            _logger.LogError("[Publishing]: Could not publish telemetry metrics: cancellation requested.");
            return;
        }

        var envelope = new MessageEnvelope<ProcessMetricsSnapshot>
        {
            Type = MessageType.TelemetrySnapshot,
            Payload = snapshot
        };

        var messageBytes = _serializer.Serialize(envelope, prefixed: true);

        if (messageBytes is null)
        {
            _logger.LogError("[Publishing]: Could not serialize a message envelope: {}", _serializer.GetError()?.Message ?? "unknown error");
            return;
        }

        await _transport.SendAsync(messageBytes, ct);
    }
}
