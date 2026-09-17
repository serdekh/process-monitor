using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

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
        if (ct.IsCancellationRequested)
        {
            _logger.LogInformation("[Host][Collection]: Could not start the service: cancellation requested");
            return;
        }
        
        _logger.LogInformation("[Host][Collection]: Starting...");
        
        var collectionResult = await _collector.RunAsync(ct);

        foreach (var warning in collectionResult.Warnings)
        {
            _logger.LogWarning("[Host][Collection]: {}", warning.ToString());
        }

        if (collectionResult is Failure<None, CollectionError, CollectionWarning> failure)
        {
            for (var it = failure.Chain; it is not null; it = it.Inner)
            { 
                _logger.LogError("[Host][Collection]: {}", it.Error);
            }
        }
        
        _logger.LogInformation("[Host][Collection]: Terminated");
    }
}
