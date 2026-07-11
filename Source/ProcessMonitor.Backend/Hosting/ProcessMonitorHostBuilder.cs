using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;

using ProcessMonitor.Backend.State;
using ProcessMonitor.Backend.Commands;
using ProcessMonitor.Backend.Publishing;
using ProcessMonitor.Backend.Processing;
using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Commands.Handlers;

using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Transport;
using ProcessMonitor.Shared.Serialization;

namespace ProcessMonitor.Backend.Hosting;

public static class ProcessMonitorHostBuilder
{
    public static HostApplicationBuilder Create(string[] args)
    { 
        var builder = Host.CreateApplicationBuilder(args);

        ConfigureLogging(builder.Logging);
        ConfigureServices(builder.Services);

        return builder;
    }

    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();

        logging.AddConsole();

        logging.AddDebug();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MonitoringSessionState>();

        services.AddSingleton(Channel.CreateUnbounded<TraceEvent>());
        services.AddSingleton(Channel.CreateUnbounded<ProcessMetricsSnapshot>());

        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
    
        services.AddSingleton<ITelemetryTransport, TelemetryTransport>();

        services.AddSingleton<IEventCollector, EventStreamCollector>();

        services.AddSingleton<EventMetricsEngine>();

        services.AddSingleton<IMetricsPublisher, IPCMetricsPublisher>();
        
        services.AddTransient<StartMonitoringHandler>();

        services.AddSingleton<CommandController>();
        services.AddSingleton<ICommandTransport, CommandTransport>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<CommandRegistry>();

        services.AddHostedService<CollectorHostedService>();
        services.AddHostedService<EngineHostedService>();
        services.AddHostedService<PublisherHostedService>();
        services.AddHostedService<CommandListenerHostedService>();
    }
} 
