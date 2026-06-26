using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using ProcessMonitor.Backend.State;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Transport;
using ProcessMonitor.Backend.Publishing;
using ProcessMonitor.Backend.Processing;
using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Serialization;

using ProcessMonitor.Contracts.Snapshots;

namespace ProcessMonitor.Backend.Hosting;

public static class ProcessMonitorHostBuilder
{
    // what is this interface
    public static HostApplicationBuilder Create(string[] args)
    { 
        // what is that method
        var builder = Host.CreateApplicationBuilder(args);

        ConfigureLogging(builder.Logging);
        ConfigureServices(builder.Services);

        return builder;
    }

    // what is that interface
    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();

        logging.AddConsole();

        logging.AddDebug();
    }

    // what is that interface
    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MonitoringSessionState>();

        services.AddSingleton(Channel.CreateUnbounded<RawKernelEvent>());
        services.AddSingleton(Channel.CreateUnbounded<ProcessMetricsSnapshot>());

        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
    
        services.AddSingleton<ITelemetryTransport, TelemetryTransport>();

        services.AddSingleton<IEventCollector, EventStreamCollector>();

        services.AddSingleton<EventMetricsEngine>();

        services.AddSingleton<IMetricsPublisher, IPCMetricsPublisher>();

        services.AddHostedService<CollectorHostedService>();
        services.AddHostedService<EngineHostedService>();
        services.AddHostedService<PublisherHostedService>();
    }
} 
