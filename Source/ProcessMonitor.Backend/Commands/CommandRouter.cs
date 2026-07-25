using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.Commands.Handlers;

using ProcessMonitor.Shared.Protocol;

namespace ProcessMonitor.Backend.Commands;

public sealed class CommandRouter(IServiceProvider sp, CommandRegistry registry)
{
    private readonly IServiceProvider _sp = sp;
    private readonly CommandRegistry _registry = registry;

    public static (MessageEnvelope<CommandResponse>, Exception?) GetError404(string route)
    {
        var invalidRequestEnvelope = new MessageEnvelope<CommandResponse>
        {
            Type = MessageType.CommandResponse,
            Payload = new CommandResponse
            {
                StatusCode = 404,
                Message = "Invalid request"
            }
        };

        return (invalidRequestEnvelope, new InvalidOperationException(invalidRequestEnvelope.Payload.Message));
    }

    public async Task<(MessageEnvelope<CommandResponse>, Exception?)> TryRouteAsync(MessageEnvelope<CommandRequest> request, CancellationToken ct)
    {
        var route = $"{request.Payload.Route}/{request.Payload.Method}";

        (var handlerType, var handlerAcquirementException)= _registry.GetHandler(route);

        if (handlerAcquirementException is not null) return (new MessageEnvelope<CommandResponse>(), handlerAcquirementException);

        Debug.Assert(handlerType is not null, "handlerType variable should not be null, otherwise there is a bug in the GetHandler method.");

        var handler = (ICommandHandler?)_sp.GetService(handlerType);

        if (handler is null) return GetError404(route);

        return  await handler.HandleAsync(request, ct);
    }
}
