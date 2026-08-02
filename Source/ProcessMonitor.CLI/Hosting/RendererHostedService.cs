using System;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Client.Hosting.Services;

namespace ProcessMonitor.CLI.Hosting;

public sealed class RendererHostedService(ClientApplicationState state) : RendererService
{
    private readonly ClientApplicationState _state = state;

    public override Task<Exception?> Render(ClientApplicationState state, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return Task.FromResult<Exception?>(new InvalidOperationException());

        if (state.LatestSnapshot is not null)
        {
            Console.WriteLine(state.LatestSnapshot);
            state.LatestSnapshot = null;
        }

        return Task.FromResult<Exception?>(null);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var renderingException = await Render(_state, stoppingToken);

            if (renderingException is not null) break;
        }
    }
}