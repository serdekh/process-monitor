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

        if (initializationResult.IsFailure())
        {
            initializationResult.AsFailure()
                .ForEachError(x => _logger.LogError("[Publishing][Error]: {}", x.ToString()));
            return;
        }

        var connectionResult = await _transport.TryConnectAsync(ct);

        if (connectionResult.IsFailure())
        {
            connectionResult.AsFailure()
                .ForEachError(x => _logger.LogError("[Publishing][Error]: {}", x.ToString()));
            return;
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

        if (serializationResult.IsFailure())
        {
            serializationResult.AsFailure()
                .ForEachError(x => _logger.LogError("[Publishing][Error]: {}", x.ToString()));
            return;
        }

        var messageBytes = serializationResult.AsSuccess().Value;

        var writingResult = await _transport.TryWriteAsync(messageBytes, ct);

        if (writingResult.IsFailure())
        {
            writingResult.AsFailure()
                .ForEachError(x => _logger.LogError("[Publishing][Error]: {}", x.ToString()));
            return;
        }
    }
}