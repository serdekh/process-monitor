using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Commands;

namespace ProcessMonitor.Backend.Hosting;

public sealed class CommandListenerHostedService : BackgroundService
{
    private ILogger<CommandListenerHostedService> _logger;

    private readonly CommandController _controller;

    public CommandListenerHostedService(
        ILogger<CommandListenerHostedService> logger,
        CommandController controller)
    {
        _logger = logger;
        _controller = controller;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        _logger.LogInformation("Command listening: Start.");

        await _controller.RunAsync(ct);

        _logger.LogInformation("Command listening: Stop.");
    }
}
