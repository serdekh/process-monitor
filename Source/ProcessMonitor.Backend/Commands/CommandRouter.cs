using System;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.Commands.Handlers;

using ProcessMonitor.Shared.Protocol;

namespace ProcessMonitor.Backend.Commands;

public sealed class CommandRouter
{
    private readonly IServiceProvider _sp;
    private readonly CommandRegistry _registry;

    public CommandRouter(IServiceProvider sp, CommandRegistry registry)
    {
        _sp = sp;
        _registry = registry;
    }

    public async Task<MessageEnvelope<CommandResponse>> RouteAsync(MessageEnvelope<CommandRequest> request, CancellationToken ct)
    {
        var handlerType = _registry.GetHandler($"{request.Payload.Route}/{request.Payload.Method}");

        var handler = (ICommandHandler?)_sp.GetService(handlerType);

        if (handler is null)
        {
            return new MessageEnvelope<CommandResponse>
            {
                Type = MessageType.CommandResponse,
                Payload = new CommandResponse
                {
                    StatusCode = 404,
                    Message = "Invalid request"
                }
            };
        }

        return await handler.HandleAsync(request, ct);
    }
}
