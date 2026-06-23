using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using ProcessMonitor.Backend.State;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Hosting;
using ProcessMonitor.Backend.Transport;
using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Processing;
using ProcessMonitor.Backend.Publishing;
using ProcessMonitor.Backend.Serialization;

using ProcessMonitor.Contracts.Snapshots;

namespace ProcessMonitor.Backend;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<MonitoringSessionState>();
        builder.Services.AddSingleton(Channel.CreateUnbounded<RawKernelEvent>());
        builder.Services.AddSingleton(Channel.CreateUnbounded<ProcessMetricsSnapshot>());
        builder.Services.AddSingleton<
            IMessageSerializer, 
            JsonMessageSerializer>();

        builder.Services.AddSingleton<
            ITelemetryTransport,
            TelemetryTransport>();

        builder.Services.AddSingleton<
            IEventCollector,
            EventStreamCollector>();

        builder.Services.AddSingleton<EventMetricsEngine>();

        builder.Services.AddSingleton<
            IMetricsPublisher,
            IPCMetricsPublisher>();

        builder.Services.AddHostedService<CollectorHostedService>();
        builder.Services.AddHostedService<EngineHostedService>();
        builder.Services.AddHostedService<PublisherHostedService>();

        await builder.Build().RunAsync();
    }
}
