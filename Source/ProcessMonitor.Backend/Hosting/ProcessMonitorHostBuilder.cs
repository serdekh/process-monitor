using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Extensions.DependencyInjection;

using ProcessMonitor.Backend.State;
using ProcessMonitor.Backend.Commands;
using ProcessMonitor.Backend.Transport;
using ProcessMonitor.Backend.Publishing;
using ProcessMonitor.Backend.Processing;
using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Commands.Handlers;

using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Shared.Client.Input.Args;

namespace ProcessMonitor.Backend.Hosting;

public static class ProcessMonitorHostBuilder
{
    public static HostApplicationBuilder Create(string[] args)
    { 
        var builder = Host.CreateApplicationBuilder(args);

        ConfigureLogging(builder.Logging);
        ConfigureServices(args, builder.Services);

        return builder;
    }

    private static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();

        logging.AddConsole();

        logging.AddDebug();
    }

    private static void ConfigureServices(string[] args, IServiceCollection services)
    {
        var argsParser = new ArgsParser();

        argsParser.Parse(args);

        services.AddSingleton(new MonitoringSessionState(argsParser.Configuration.ProcessId ?? 0));

        services.AddSingleton(Channel.CreateUnbounded<RawEvent>());
        services.AddSingleton(Channel.CreateUnbounded<ProcessMetricsSnapshot>());

        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        services.AddSingleton<IEventCollector, EventStreamCollector>();

        services.AddSingleton<EventMetricsEngine>();

        services.AddSingleton<IMetricsPublisher, IPCMetricsPublisher>();
        
        services.AddTransient<StartMonitoringHandler>();
        services.AddTransient<StopMonitoringHandler>();

        services.AddSingleton<CommandController>();
        services.AddTransient<ITransportServer, TransportServer>();
        services.AddSingleton<CommandRouter>();
        services.AddSingleton<CommandRegistry>();

        services.AddHostedService<CollectorHostedService>();
        services.AddHostedService<EngineHostedService>();
        services.AddHostedService<PublisherHostedService>();
        services.AddHostedService<CommandListenerHostedService>();
    }
} 
