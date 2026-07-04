using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Collection;

namespace ProcessMonitor.Backend.Hosting;

public sealed class CollectorHostedService : BackgroundService
{
    private ILogger<CollectorHostedService> _logger;

    private IEventCollector _collector;

    public CollectorHostedService(
        ILogger<CollectorHostedService> logger,
        IEventCollector collector)
    { 
        _logger = logger;
        _collector = collector;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!ct.IsCancellationRequested)
        {
            _logger.LogInformation("Collection: Starting...");
            
            await _collector.RunAsync(ct);
            
            _logger.LogInformation("Collection: Terminating...");
        }
        else
        {
            _logger.LogInformation("Collection: Could not start the service: cancellation requested.");
        }
    }
}
