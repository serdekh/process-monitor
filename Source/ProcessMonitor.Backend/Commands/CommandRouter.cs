using System;
using System.Threading;
using System.Threading.Tasks;

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

    public async Task<CommandResponse> RouteAsync(CommandRequest request, CancellationToken ct)
    {
        var handlerType = _registry.GetHandler(request.Route);

        var handler = (ICommandHandler?)_sp.GetService(handlerType);

        if (handler is null)
        {
            // TODO: Handle this situation
            throw new NotImplementedException();
        }

        return await handler.HandleAsync(request, ct);
    }
}
