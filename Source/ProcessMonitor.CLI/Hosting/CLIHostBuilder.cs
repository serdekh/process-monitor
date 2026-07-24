using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using ProcessMonitor.CLI.State;
using ProcessMonitor.CLI.Common;

using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Transport.Framing;

namespace ProcessMonitor.CLI.Hosting;

public sealed class CLIHostBuilder
{
    public static HostApplicationBuilder Create(string[] args)
    { 
        var builder = Host.CreateApplicationBuilder(args);

        ConfigureLogging(builder.Logging);
        ConfigureServices(builder.Services);

        return builder;
    }

    public static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();

        logging.AddConsole();

        logging.AddDebug();
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<RuntimeState>();

        services.AddSingleton<IFrameReader, FrameReader>();
        services.AddSingleton<IFrameWriter, FrameWriter>();

        services.AddSingleton<BackendProcess>();

        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        services.AddHostedService<InputReaderHostedService>();
    }
}