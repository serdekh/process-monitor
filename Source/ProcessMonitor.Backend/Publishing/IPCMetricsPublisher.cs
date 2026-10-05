using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Models.Errors.Transport;
using ProcessMonitor.Backend.Models.Warnings.Transport;
using ProcessMonitor.Backend.Transport;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Errors.Serialization;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Models.Warnings.Serialization;
using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Snapshots;

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
        var initializationResult = _transport.TryInitialize(TransportServerOptions.CreateDefaultTelemetryPipe());

        if (initializationResult is Failure<None, TransportError, TransportWarning> initializationFailure)
        {
            _logger.LogError("[Publishing]: Failed to initialize a telemetry server stream: {}",
                initializationFailure.Chain.Error.ToString());
            return;
        }

        var connectionResult = await _transport.TryConnectAsync(ct);

        if (connectionResult is Failure<None, TransportError, TransportWarning> connectionFailure)
        {
            _logger.LogError("[Publishing]: Failed to connect via the telemetry pipe: {}",
                connectionFailure.Chain.Error.ToString());
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

        var serializationResult = _serializer.TrySerialize(envelope);

        if (serializationResult is Failure<byte[], SerializationError, SerializationWarning> failure)
        {
            _logger.LogError("[Publishing]: Could not serialize a message envelope: {}.", failure.Chain.Error.ToString());
            return;
        }

        var messageBytes = ((Success<byte[], SerializationError, SerializationWarning>)serializationResult).Value;

        var writingResult = await _transport.TryWriteAsync(messageBytes, ct);

        if (writingResult is Failure<None, TransportError, TransportWarning> writingFailure)
        {
            _logger.LogError("[Publishing]: Could not write a message envelope: {}.",
                writingFailure.Chain.Error.ToString());
        }
    }
}