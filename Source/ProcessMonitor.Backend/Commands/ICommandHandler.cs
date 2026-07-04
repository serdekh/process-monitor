using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Protocol;

namespace ProcessMonitor.Backend.Commands;

public interface ICommandHandler
{
    public Task<CommandResponse> HandleAsync(CommandRequest request, CancellationToken ct);
}
