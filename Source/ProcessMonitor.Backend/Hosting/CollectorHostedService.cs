using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using ProcessMonitor.Backend.Collection;

namespace ProcessMonitor.Backend.Hosting;

public sealed class CollectorHostedService : BackgroundService
{
    private IEventCollector _collector;

    public CollectorHostedService(IEventCollector collector)
    {
        _collector = collector;
    }

    // TODO: Handle cancellation token exception
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!ct.IsCancellationRequested)
        {
            await _collector.RunAsync(ct);
        }

        Console.WriteLine("Collector service is cancelled");
    }
}
