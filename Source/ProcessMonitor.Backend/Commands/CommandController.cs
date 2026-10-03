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

// TODO: Replace immediate logs with custom exception? return values
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

            if (readingResult is Failure<byte[], TransportError, TransportWarning> readingFailure)
            {
                _logger.LogError("Command listening: Could not read from the client: {}. Stop.",
                    readingFailure.Chain.Error.ToString());
                break;
            }

            var bytes = ((Success<byte[], TransportError, TransportWarning>)readingResult).Value;

            (var request, var deserializationException) = _serializer.TryDeserialize<MessageEnvelope<CommandRequest>>(bytes); if (deserializationException is not null)
            {
                _logger.LogError("Command listening: Failed to deserialize request: {}. Stop.", deserializationException.Message);
                break;
            }
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

            (var responseBytes, var serializationException) = _serializer.TrySerialize(response); if (serializationException is not null)
            {
                _logger.LogError("Command listening: Failed to serialize a response object. Stop.");
                break;
            }

            var writingResult = await _transport.TryWriteAsync(responseBytes, ct);

            if (writingResult is Failure<None, TransportError, TransportWarning> writingFailure)
            {
                _logger.LogError("Command listening: Failed to write a message: {}. Stop.",
                    writingFailure.Chain.Error.ToString());
                break;
            }
        }

        _logger.LogInformation("Command listening: Terminating...");

        var deinitializationResult = await _transport.DeinitializeAsync();

        foreach (var warning in deinitializationResult.Warnings)
        {
            _logger.LogWarning("Command listening: {}", warning.ToString());
        }

        _logger.LogInformation("Command listening: Terminated.");
    }
}