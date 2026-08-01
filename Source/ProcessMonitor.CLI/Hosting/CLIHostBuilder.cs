using System;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Transport.Framing;
using ProcessMonitor.Shared.Client.Input.Args;

namespace ProcessMonitor.CLI.Hosting;

public sealed class CLIHostBuilder
{
    public static (HostApplicationBuilder, Exception?) Create(string[] args)
    { 
        var argsParser = new ArgsParser();

        var parsingException = argsParser.Parse(args);

        var builder = Host.CreateApplicationBuilder(args);

        if (parsingException is not null) return (builder, parsingException);

        ConfigureLogging(builder.Logging);

        ConfigureServices(builder.Services, argsParser.Configuration);

        return (builder, null);
    }

    public static void ConfigureLogging(ILoggingBuilder logging)
    {
        logging.ClearProviders();

        logging.AddConsole();

        logging.AddDebug();
    }

    public static void ConfigureServices(IServiceCollection services, ClientApplicationConfiguration configuration)
    {
        services.AddSingleton<IFrameReader, FrameReader>();
        services.AddSingleton<IFrameWriter, FrameWriter>();

        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        services.Configure<ClientApplicationConfiguration>(config =>
        {
            config.ProcessId = configuration.ProcessId;
            config.ServerFilepath = configuration.ServerFilepath;
        });

        services.AddSingleton<ClientApplicationState>();

        services.AddHostedService<InputReaderHostedService>();
    }
}