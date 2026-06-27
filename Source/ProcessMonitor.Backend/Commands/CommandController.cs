using System;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.Transport;
using ProcessMonitor.Backend.Serialization;

using ProcessMonitor.Contracts.Protocol;

namespace ProcessMonitor.Backend.Commands;

public sealed class CommandController
{
    private readonly ICommandTransport _transport;
    private readonly IMessageSerializer _serializer;
    private readonly CommandRouter _router;

    public CommandController(ICommandTransport transport, 
                             IMessageSerializer serializer, 
                             CommandRouter router)
    {
        _transport = transport; 
        _serializer = serializer;
        _router = router;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await _transport.InitializeAsync(ct);

                while (!ct.IsCancellationRequested)
                {
                    var bytes = await _transport.ReceiveAsync(ct);
                    
                    if (bytes is null)
                    {                       
                        break;
                    }

                    var request = _serializer.Deserialize<CommandRequest>(bytes);

                    if (request is null)
                    {
                        continue; 
                    }
                    
                    var response = await _router.RouteAsync(request, ct);
         
                    var responseBytes = _serializer.Serialize(response);
                    await _transport.SendAsync(responseBytes, ct);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
            }
        }
    }
}