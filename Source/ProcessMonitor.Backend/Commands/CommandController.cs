using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.Transport;
using ProcessMonitor.Backend.Serialization;

using Microsoft.Extensions.Logging;

using ProcessMonitor.Contracts.Protocol;

namespace ProcessMonitor.Backend.Commands;

public sealed class CommandController
{
    private ILogger<CommandController> _logger;
    private readonly ICommandTransport _transport;
    private readonly IMessageSerializer _serializer;
    private readonly CommandRouter _router;

    public CommandController(ILogger<CommandController> logger,
                             ICommandTransport transport, 
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

        try 
        {
            _logger.LogInformation("Command listening: Waiting for a client...");
        
            await _transport.InitializeAsync(ct);

            _logger.LogInformation("Command listening: Client connected successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Command listening: No client has connected or the stream is already taken. Stop.");
            return;
        }
    
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var bytes = await _transport.ReceiveAsync(ct);
       
                if (bytes is null)
                {
                    _logger.LogError("Command listening: Could not read from the client. Stop.");                       
                    break;
                }

                var request = _serializer.Deserialize<CommandRequest>(bytes);

                if (request is null)
                {
                    _logger.LogError("Command listening: The request has been corrupted. Stop.");
                    break;
                }
                    
                var response = await _router.RouteAsync(request, ct);
         
                var responseBytes = _serializer.Serialize(response);
            
                await _transport.SendAsync(responseBytes, ct);
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
            catch (Exception)
            {
                _logger.LogError("Command listening: Fatal error occured while processing the command. Stop.");
                break;
            }

        }    

        _transport.Deinitialize();        
    }
}
