using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Transport;

using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Protocol;

namespace ProcessMonitor.Backend.Publishing;

public sealed class IPCMetricsPublisher : IMetricsPublisher, IDisposable
{
    private readonly IMessageSerializer _serializer;
    private readonly ITransportServer _transport;

    private readonly ILogger<IPCMetricsPublisher> _logger;

    public IPCMetricsPublisher(
        IMessageSerializer serializer, 
        ITransportServer transport,
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
        var initializationException = _transport.TryInitialize(
            "ProcessMonitor.Pipes.Telemetry",
            PipeDirection.Out,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous
        );

        if (initializationException is not null)
        {
            _logger.LogError("[Publishing]: Failed to initialize a telemetry server stream: {}", initializationException.Message);
            return;
        }

        var connectionException = await _transport.TryConnectAsync(ct);

        if (connectionException is not null)
        {
            _logger.LogError("[Publishing]: Failed to connect via the telemetry pipe: {}", connectionException.Message);
        }
    }

    public void Deinitialize()
    {
        _transport.DeinitializeAsync();
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

        (var messageBytes, var serializationException) = _serializer.TrySerialize(envelope);

        if (serializationException is not null)
        {
            _logger.LogError("[Publishing]: Could not serialize a message envelope: {}.", serializationException.Message);
            return;
        }

        var writingException = await _transport.TryWriteAsync(messageBytes, ct);

        if (writingException is not null)
        {
            _logger.LogError("[Publishing]: Could not write a message envelope: {}.", writingException.Message);
        }
    }
}
