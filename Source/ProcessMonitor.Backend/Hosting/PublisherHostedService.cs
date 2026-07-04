using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Publishing;

using ProcessMonitor.Shared.Snapshots;

namespace ProcessMonitor.Backend.Hosting;

public sealed class PublisherHostedService : BackgroundService
{
    private readonly ChannelReader<ProcessMetricsSnapshot> _input;
    private readonly IMetricsPublisher _publisher;

    private readonly ILogger<PublisherHostedService> _logger;

    public PublisherHostedService(
        Channel<ProcessMetricsSnapshot> input,
        IMetricsPublisher publisher,
        ILogger<PublisherHostedService> logger)
    {
        _input = input.Reader;
        _publisher = publisher;
        _logger =logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogInformation("[Host][Publishing]: Could not start the service: cancellation requested.");
            return;
        }

        _logger.LogInformation("[Host][Publishing]: Starting...");

        while (!ct.IsCancellationRequested)
        {
            var snapshot = await _input.ReadAsync(ct);

            await _publisher.PublishAsync(snapshot, ct);
        }

        _logger.LogInformation("[Host][Publishing]: Terminating...");
    }
}
