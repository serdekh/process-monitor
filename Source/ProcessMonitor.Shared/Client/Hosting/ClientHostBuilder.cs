using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Transport.Framing;
using ProcessMonitor.Shared.Client.Input.Args;
using ProcessMonitor.Shared.Client.Hosting.Services;

namespace ProcessMonitor.Shared.Client.Hosting;

public sealed class ClientHostBuilder
{
    private readonly string[] _args;

    private readonly ArgsParser _argsParser;

    public Exception? Failed { get; private set; }

    public HostApplicationBuilder? Builder { get; private set; }

    public IHost? ClientHost { get; private set; }

    public ClientHostBuilder(string[] args)
    {
        _args = args;
        _argsParser = new ArgsParser();

        Failed = _argsParser.Parse(args);

        if (Failed is null)
        {
            Builder = Host.CreateApplicationBuilder(_args);
        }
    }

    public ClientHostBuilder Build()
    { 
        if (Failed is not null || Builder is null) return this;

        ClientHost = Builder.Build();

        return this;
    }

    public async Task RunAsync()
    {
        if (ClientHost is null) return;

        var ctSource = new CancellationTokenSource();

        await ClientHost.RunAsync(ctSource.Token);
    }

    public ClientHostBuilder UseLogging()
    {
        if (Builder is null) return this;

        Builder.Logging.ClearProviders();

        Builder.Logging.AddConsole();

        Builder.Logging.AddDebug();

        return this;
    }

    public ClientHostBuilder UseCore()
    {
        if (Builder is null) return this;

        Builder.Services.AddSingleton<IFrameReader, FrameReader>();
        Builder.Services.AddSingleton<IFrameWriter, FrameWriter>();

        Builder.Services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

        Builder.Services.Configure<ClientApplicationConfiguration>(config =>
        {
            config.ProcessId = _argsParser.Configuration.ProcessId;
            config.ServerFilepath = _argsParser.Configuration.ServerFilepath;
        });

        Builder.Services.AddSingleton<ClientApplicationState>();

        return this;
    }

    public ClientHostBuilder UseInputReader<TInputReader>() where TInputReader : InputReaderService
    {
        if (Builder is null) return this;

        Builder.Services.AddHostedService<TInputReader>();

        return this; 
    }

    public ClientHostBuilder UseRenderer<TRenderer>() where TRenderer : RendererService
    {
        if (Builder is null) return this;

        Builder.Services.AddHostedService<TRenderer>();

        return this; 
    }
}