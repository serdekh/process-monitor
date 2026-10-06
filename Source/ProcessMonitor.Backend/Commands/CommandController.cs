using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Models.Errors.Transport;
using ProcessMonitor.Backend.Models.Warnings.Transport;
using ProcessMonitor.Backend.Transport;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Serialization;

namespace ProcessMonitor.Backend.Commands;

public sealed class CommandController(ILogger<CommandController> logger,
                         ITransportServer transport,
                         IMessageSerializer serializer,
                         CommandRouter router)
{
    private readonly ILogger<CommandController> _logger = logger;
    private readonly ITransportServer _transport = transport;
    private readonly IMessageSerializer _serializer = serializer;
    private readonly CommandRouter _router = router;

    public async Task RunAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        _logger.LogInformation("Command listening: Waiting for a client...");

        var initializationResult = _transport.TryInitialize(TransportServerOptions.CreateDefaultCommandsPipe());

        if (initializationResult is Failure<None, TransportError, TransportWarning> initializationFailure)
        {
            _logger.LogError("Command listening: Failed to initialize a server stream: {}.",
                initializationFailure.Chain.Error.ToString());
            return;
        }

        var connectionResult = await _transport.TryConnectAsync(ct);

        if (connectionResult is Failure<None, TransportError, TransportWarning> connectionFailure)
        {
            _logger.LogError("Command listening: Failed to connect to a client: {}.",
                connectionFailure.Chain.Error.ToString());
            return;
        }

        _logger.LogInformation("Command listening: Client connected successfully.");

        while (!ct.IsCancellationRequested)
        {
            var readingResult = await _transport.TryReadAsync(ct);

            if (readingResult.IsFailure())
            {
                _logger.LogError("Command listening: Could not read from the client: {}. Stop.",
                    readingResult.AsFailure().Chain.Error.ToString());
                break;
            }

            var bytes = readingResult.AsSuccess().Value;

            var deserializingResult = _serializer.TryDeserialize<MessageEnvelope<CommandRequest>>(bytes);

            if (deserializingResult.IsFailure())
            {
                _logger.LogError("Command listening: Failed to deserialize request: {}. Stop.",
                    deserializingResult.AsFailure().Chain.Error.ToString());
                break;
            }

            var request = deserializingResult.AsSuccess().Value;

            if (request is null)
            {
                _logger.LogError("Command listening: The request has been corrupted. Stop.");
                break;
            }

            (var response, var routingException) = await _router.TryRouteAsync(request, ct); if (routingException is not null)
            {
                _logger.LogError("Command listening: Could not read from the client: {}. Stop.", routingException.Message);
                break;
            }

            var serializingResult = _serializer.TrySerialize(response);

            if (serializingResult.IsFailure())
            {
                _logger.LogError("Command listening: Failed to serialize a response object: {}. Stop.",
                    serializingResult.AsFailure().Chain.Error.ToString());
                break;
            }

            var responseBytes = serializingResult.AsSuccess().Value;

            var writingResult = await _transport.TryWriteAsync(responseBytes, ct);

            if (writingResult.IsFailure())
            {
                _logger.LogError("Command listening: Failed to write a message: {}. Stop.",
                    writingResult.AsFailure().Chain.Error.ToString());
                break;
            }
        }

        _logger.LogInformation("Command listening: Terminating...");

        var deinitializationResult = await _transport.DeinitializeAsync();

        deinitializationResult.ForEachWarning(x => _logger.LogWarning("Command listening: {}", x.ToString()));

        _logger.LogInformation("Command listening: Terminated.");
    }
}