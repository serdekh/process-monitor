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

public sealed class CommandController
{
    private ILogger<CommandController> _logger;
    private readonly ITransportServer _transport;
    private readonly IMessageSerializer _serializer;
    private readonly CommandRouter _router;

    public CommandController(ILogger<CommandController> logger,
                             ITransportServer transport, 
                             IMessageSerializer serializer, 
                             CommandRouter router)
    {
        _logger = logger;
        _transport = transport; 
        _serializer = serializer;
        _router = router;
    }

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
            try
            {
                (var bytes, var readingException) = await _transport.TryReadAsync(ct);
       
                if (readingException is not null)
                {
                    _logger.LogError("Command listening: Could not read from the client: {}. Stop.", readingException.Message);                       
                    break;
                }

                (var request, var deserializationException) = _serializer.TryDeserialize<MessageEnvelope<CommandRequest>>(bytes);

                if (deserializationException is not null)
                {
                    _logger.LogError("Command listening: Failed to deserialize request: {}. Stop.", deserializationException.Message);
                    break;
                }

                if (request is null)
                {
                    _logger.LogError("Command listening: The request has been corrupted. Stop.");
                    break;
                }
                    
                // TODO: add a TryRouteAsync method to handle the exception
                // and remove the outer try-catch block from this method
                var response = await _router.RouteAsync(request, ct);
         
                (var responseBytes, var serializationException) = _serializer.TrySerialize(response);

                if (serializationException is not null)
                {
                    _logger.LogError("Command listening: Failed to serialize a response object. Stop.");
                    break;
                }
            
                var writingException = await _transport.TryWriteAsync(responseBytes, ct);

                if (writingException is not null)
                {
                    _logger.LogError("Command listening: Failed to write a message: {}. Stop.", writingException.Message);
                    break;
                }
            }
            catch (IOException)
            {
                _logger.LogError("Command listening: Could not process a command. The pipe is broken or read through. Stop.");
                break;
            }
            catch (OperationCanceledException)
            {
                _logger.LogError("Command listening: Could not finish processing the command due to a cancellation request. Stop.");
                break;
            }
            catch (ObjectDisposedException)
            {
                _logger.LogError("Command listening: Could not finish processing the command due to one of the data streams getting disposed. Stop.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError("Command listening: Fatal error occured while processing the command. Stop.");
                break;
            }
        }    

        _logger.LogInformation("Command listening: Terminating...");

        await _transport.DeinitializeAsync();       

        _logger.LogInformation("Command listening: Terminated."); 
    }
}
