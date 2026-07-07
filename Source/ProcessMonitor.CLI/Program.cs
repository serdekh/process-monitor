using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

using ProcessMonitor.CLI.Input;
using ProcessMonitor.CLI.Common;
using ProcessMonitor.CLI.Hosting;

namespace ProcessMonitor.CLI;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var argsParser = new CommandLineParser(args);

        if (!argsParser.Parse())
        {
            argsParser.PrintErrors();
            return;
        }

        // TODO: Replace mannual '--path' flag insertion with a custom config file
        if (!argsParser.Flags.TryGetValue("--path", out object? value))
        {
            Console.WriteLine("procmon: error: The filepath to the backend process was not specified.\nProvide the '--path <file-path>' flag in the command line arguments.");
            return;
        }

        if (value is not string path) return;

        var builder = CLIHostBuilder 
            .Create(args);

        // NOTE: Consider replacing this code with a refactorred logic inside the ConsoleInputReader class
        // so that we don't explicitly inject this path string. For example, add a new hosted service that
        // manages cli arguments and exposes them to the other services inside the ConfigureServices method. 
        builder.Services.Configure<BackendProcessOptions>(options =>
        {
            options.FilePath = path;
        });

        await builder.Build().RunAsync();
    }
}
