using System;
using System.Threading.Tasks;

using ProcessMonitor.CLI.Hosting;

using ProcessMonitor.Shared.Client.Hosting;

namespace ProcessMonitor.CLI;

internal class Program
{
    public static async Task Main(string[] args)
    {
        if (!Environment.IsPrivilegedProcess)
        {
            Console.WriteLine($"[ProcessMonitor]: error: Could only run as administrator.");
            return;
        }

        var builder = new ClientHostBuilder(args);

        builder
            .UseCore()
            .UseLogging()
            .UseInputReader<InputReaderHostedService>()
            .UseRenderer<RendererHostedService>()
            .Build();

        if (builder.Failed is not null)
        {
            Console.WriteLine($"[ProcessMonitor]: error: Failed to initialize the application: {builder.Failed.Message}.");
            return;
        }
        
        await builder.RunAsync();
    }
}
