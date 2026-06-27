using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using ProcessMonitor.Backend.Commands;

namespace ProcessMonitor.Backend.Hosting;

public sealed class CommandListenerHostedService : BackgroundService
{
    private readonly CommandController _controller;

    public CommandListenerHostedService(CommandController controller)
    {
        _controller = controller;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await _controller.RunAsync(ct);
    }
}
