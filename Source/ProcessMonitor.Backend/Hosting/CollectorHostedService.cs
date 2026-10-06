using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Hosting;

public sealed class CollectorHostedService : BackgroundService
{
    private readonly ILogger<CollectorHostedService> _logger;

    private readonly IEventCollector _collector;

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

        collectionResult.ForEachWarning(x => _logger.LogWarning("[Host][Collection]: {}", x.ToString()));

        if (collectionResult.IsFailure())
        {
            collectionResult.AsFailure().ForEachError(x => _logger.LogError("[Host][Collection]: {}", x));
        }

        _logger.LogInformation("[Host][Collection]: Terminated");
    }
}