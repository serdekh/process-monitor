using System;
using System.IO;
using System.Collections.Generic;

namespace ProcessMonitor.CLI.Input;

public sealed class CommandLineParser
{
    public readonly Dictionary<string, object?> Flags = [];

    private delegate bool FlagHandler(string[] args, ref int args_i);

    private Dictionary<string, FlagHandler> _handlers;

    private string[]? _args;

    public List<string> ErrorMessages { get; private set; } = [];

    public bool HasError
    {
        get
        {
            return ErrorMessages.Count > 0;
        }
    }

    public CommandLineParser(string[]? args = null)
    {
        _handlers = new Dictionary<string, FlagHandler>
        {
            ["--pid"] = ParsePid,
            ["--path"] = ParsePath,
        };

        _args = args;
    }

    public void PrintErrors()
    {
        foreach (var error in ErrorMessages)
        {
            Console.WriteLine($"procmon: error: {error}.");
        }
    }

    private bool ParsePath(string[] args, ref int args_i)
    {
        var flag = args[args_i];

        if (Flags.ContainsKey(flag))
        {
            ErrorMessages.Add($"Redefinition of the '{flag}' flag");
            return false;
        }

        if (args_i + 1 >= args.Length)
        {
            ErrorMessages.Add($"Not enough arguments for the '{flag}' flag.\nUsage: {flag} <file-path>");
            return false;
        }

        if (!File.Exists(args[args_i + 1]))
        {
            ErrorMessages.Add($"Could not find a file at '{args[args_i + 1]}' for the '{flag}' flag\nUsage: --{flag} <file-path>");
            return false;
        }

        Flags.Add(flag, args[args_i + 1]);

        args_i++;

        return true;
    }

    private bool ParsePid(string[] args, ref int args_i)
    {
        var flag = args[args_i];

        if (Flags.ContainsKey(flag))
        {
            ErrorMessages.Add($"Redefinition of the '{flag}' flag");
            return false;
        }

        if (args_i + 1 >= args.Length)
        {
            ErrorMessages.Add($"Not enough arguments for the '{flag}' flag.\nUsage: --pid <int>");
            return false;
        }

        if (!int.TryParse(args[args_i + 1], out int pid))
        {
            ErrorMessages.Add($"Could convert the input argument ('{args[args_i + 1]}') of the '{flag}' flag into an integer\nUsage: --pid <int>");
            return false;
        }

        args_i++;

        Flags.Add(flag, pid);

        return true;
    }

    public bool Parse(string[] args)
    {
        ErrorMessages.Clear();

        for (int i = 0; i < args.Length; i++)
        {
            if (!_handlers.TryGetValue(args[i], out FlagHandler? flagHandler))
            {
                ErrorMessages.Add($"Could not parse the input flag: '{args[i]}'");
                return false;
            }

            if (!flagHandler(args, ref i))
            {
                return false;
            }
        }

        return true;
    }

    public bool Parse()
    {
        if (_args is null) 
        {
            ErrorMessages.Add("No input args were provided in the constructor");
            return false;
        }

        return Parse(_args);
    }
}
