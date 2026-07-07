using System;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.CLI.Input;
using ProcessMonitor.CLI.Transport;

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

        if (value is not string path)
        {
            return;
        }

        var reader = new ConsoleInputReader(path);

        using var cts = new CancellationTokenSource();

        var ct = cts.Token;

        await reader.ReadAsync(ct);
    }
}
