using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;

using ProcessMonitor.Backend.Publishing;

using ProcessMonitor.Contracts.Snapshots;

namespace ProcessMonitor.Backend.Hosting;

public sealed class PublisherHostedService : BackgroundService
{
    private ChannelReader<ProcessMetricsSnapshot> _input;
    private IMetricsPublisher _publisher;

    public PublisherHostedService(
        Channel<ProcessMetricsSnapshot> input,
        IMetricsPublisher publisher)
    {
        _input = input.Reader;
        _publisher = publisher;
    }

    // TODO: Handle cancellation token exception
    // TODO: Handle channel exception
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var snapshot = await _input.ReadAsync(ct);

            await _publisher.PublishAsync(snapshot, ct);
        }

        Console.WriteLine("Publishing service is cancelled");
    }
}
