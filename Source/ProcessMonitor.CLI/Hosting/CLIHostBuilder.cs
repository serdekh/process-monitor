using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using ProcessMonitor.CLI.Input;
using ProcessMonitor.CLI.Common;
using ProcessMonitor.CLI.Transport;

using ProcessMonitor.Shared.Serialization;
using Microsoft.Extensions.Options;

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
        services.AddSingleton<CommandLineParser>();

        services.AddSingleton<ConsoleInputReader>();

        services.AddSingleton<BackendProcess>();

        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        services.AddSingleton<CommandPipeClient>();

        services.AddHostedService<InputReaderHostedService>();
    }
}