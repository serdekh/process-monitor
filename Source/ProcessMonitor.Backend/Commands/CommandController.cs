using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Transport;

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
    
        var initializationException = _transport.TryInitialize(
            pipeName:                   "ProcessMonitor.Pipes.Commands",
            direction:                  PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            transmissionMode:           PipeTransmissionMode.Byte,
            options:                    PipeOptions.Asynchronous);

        if (initializationException is not null)
        {
            _logger.LogError("Command listening: Failed to initialize a server stream: {}.", initializationException.Message);
            return;
        }

        var connectionException = await _transport.TryConnectAsync(ct);

        if (connectionException is not null)
        {
            _logger.LogError("Command listening: Failed to connect to a client: {}.", connectionException.Message);
            return;
        }

        _logger.LogInformation("Command listening: Client connected successfully.");
    
        while (!ct.IsCancellationRequested)
        {
            (var bytes, var readingException) = await _transport.TryReadAsync(ct); if (readingException is not null)
            {
                _logger.LogError("Command listening: Could not read from the client: {}. Stop.", readingException.Message);                       
                break;
            }

            (var request, var deserializationException) = _serializer.TryDeserialize<MessageEnvelope<CommandRequest>>(bytes); if (deserializationException is not null)
            {
                _logger.LogError("Command listening: Failed to deserialize request: {}. Stop.", deserializationException.Message);
                break;
            } if (request is null)
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
            
            var writingException = await _transport.TryWriteAsync(responseBytes, ct); if (writingException is not null)
            {
                _logger.LogError("Command listening: Failed to write a message: {}. Stop.", writingException.Message);
                break;
            }
        }    

        _logger.LogInformation("Command listening: Terminating...");

        await _transport.DeinitializeAsync();       

        _logger.LogInformation("Command listening: Terminated."); 
    }
}
