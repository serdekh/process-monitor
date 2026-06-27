using System;

namespace ProcessMonitor.CLI.Input;

public sealed class CommandLineParser
{
    public string ErrorMessage { get; private set; } = string.Empty;

    public int? ParseProcessId(string[] args)
    {
        ErrorMessage = string.Empty;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].Equals("--pid"))
            {
                if (i + 1 >= args.Length) 
                {
                    ErrorMessage = "The `--pid` flag is missing its argument.\nUsage: `--pid <process-id>`.";                   
                    return null;
                }

                var inputArgument = args[i + 1];

                if (!Int32.TryParse(inputArgument, out int processId))
                {
                    ErrorMessage = $"The argument for the `--pid` flag (`{inputArgument}`) is not an integer.";
                    return null;
                }

                return processId;
            }
        }

        return null;
    }
}
