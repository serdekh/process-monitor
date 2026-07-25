using System;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Protocol;

namespace ProcessMonitor.Backend.Commands.Handlers;

public interface ICommandHandler
{
    public Task<(MessageEnvelope<CommandResponse>, Exception?)> HandleAsync(MessageEnvelope<CommandRequest> request, CancellationToken ct);
}
