using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Processing;

namespace ProcessMonitor.Backend.Hosting;

public sealed class EngineHostedService : BackgroundService
{
    private readonly EventMetricsEngine _engine;

    private readonly ILogger<EventMetricsEngine> _logger;

    public EngineHostedService(EventMetricsEngine engine, ILogger<EventMetricsEngine> logger)
    {
        _engine = engine;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogInformation("[Host][Processing]: Could not start the service: cancellation requested.");
            return;
        }

        _logger.LogInformation("[Host][Processing]: Starting...");

        await _engine.RunAsync(ct);

        _logger.LogInformation("[Host][Processing]: Terminating...");
    }
}
