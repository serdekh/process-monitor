using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using ProcessMonitor.Backend.Processing;

namespace ProcessMonitor.Backend.Hosting;

public sealed class EngineHostedService : BackgroundService
{
    private EventMetricsEngine _engine;

    public EngineHostedService(EventMetricsEngine engine)
    {
        _engine = engine;
    }

    // TODO: Handle cancellation token exception
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!ct.IsCancellationRequested)
        {
            await _engine.RunAsync(ct);
        }

        Console.WriteLine("Processing service is cancelled");
    }
}
