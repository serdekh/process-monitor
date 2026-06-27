using System;
using System.Threading.Tasks;

using ProcessMonitor.CLI.Input;

namespace ProcessMonitor.CLI;

internal class Program
{
    public static async Task Main(string[] args)
    {
        var argsParser = new CommandLineParser();

        // Temporary. Remove as soon as the parsing mechanism more flags is implemented
        if (args.Length != 0)
        {
            int? processId = argsParser.ParseProcessId(args);

            if (argsParser.ErrorMessage != string.Empty)
            {
                Console.WriteLine($"procmon-cli: error: {argsParser.ErrorMessage}");
                return;
            }
        }

        var reader = new ConsoleInputReader();

        await reader.ReadAsync();
    }
}
