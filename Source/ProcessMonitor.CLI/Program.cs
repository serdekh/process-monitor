using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using ProcessMonitor.CLI.Hosting;

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

        (var builder, var parsingException) = CLIHostBuilder.Create(args);

        if (parsingException is not null)
        {
            Console.WriteLine($"[ProcessMonitor]: error: Could not parse input arguments: {parsingException.Message}");
            return;
        }

        await builder.Build().RunAsync();
    }
}
