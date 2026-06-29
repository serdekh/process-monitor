using System;

using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using ProcessMonitor.CLI.Transport;

namespace ProcessMonitor.CLI.Input;

public enum CommandType
{
    None = 0,
    Help,
    Create,
    Exit,
}

public sealed class Command
{
    public CommandType Operation { get; set; } = CommandType.None;

    public object[]? Args { get; set; } = null;

    public Command(CommandType operation = CommandType.None, object[]? args = null)
    {
        Operation = operation;
        Args = args;
    }
}

public sealed class ConsoleInputReader
{
    private CommandPipeClient _client;

    private Dictionary<string, CommandType> _map;

    public ConsoleInputReader(string backendFilePath)
    {
        _map = new Dictionary<string, CommandType>()
        {
            ["help"] = CommandType.Help,
            ["h"] = CommandType.Help,
            ["create"] = CommandType.Create,
            ["exit"] = CommandType.Exit,
            ["q"] = CommandType.Exit,
        };
  
        _client = new CommandPipeClient(backendFilePath);
    }

    private async Task<string[]?> LexInput()
    {
        string? input = await Task.Run(() => Console.ReadLine());

        if (string.IsNullOrWhiteSpace(input)) return null;

        string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries |
                                          StringSplitOptions.TrimEntries);
        return words;
    }

    // TODO: handle errors using logging
    private async Task<Command?> BuildCommandToken()
    {
        var tokens = await LexInput();

        if (tokens is null) return null;

        if (!_map.ContainsKey(tokens[0])) return null;

        var commandType = _map[tokens[0]];

        var command = new Command(operation: commandType, args: null);

        switch (commandType)
        {
            case CommandType.Help:
                return command;

            case CommandType.Create: 
                return command;

            case CommandType.Exit:
                var exitCode = 0;

                if (tokens.Length > 1)  
                {
                    if (!Int32.TryParse(tokens[1], out exitCode))
                    {
                        return null;
                    }
                }
                    
                command.Args = new object[1] {(object)exitCode};
              
                return command;

            default: 
                return null;
        }
    }

    private void PrintUsage()
    {
        Console.WriteLine("`procmon-cli` is a console client that lets you communicate with the metrics server.");
        Console.WriteLine("To interact with the api, use a predefined set of commands listed below:\n");
        Console.WriteLine("\thelp|h       - get this usage message.");
        Console.WriteLine("\tcreate       - start up the ProcessMonitor.Backend server process.");
        Console.WriteLine("\texit <code?> - exit the client process with the `<code>` exit status");
        Console.WriteLine("\t               if provided. Otherwise exit with `0`.");
        Console.WriteLine("\tq              - exit with `0` exit code.");
    }

    // Note: this method assumes the input arguments are
    // valid since they're handled by the BuildCommandToken method
    private async Task RunCommand(Command command)
    {
        switch (command.Operation)
        {
            case CommandType.None:
                Console.WriteLine("Could not recognize a command. Skip.\nRun the `help` or `h` command to get more information.");
                return;
            case CommandType.Help:
                PrintUsage();
                return;
            case CommandType.Create:
                if (_client.IsConnected) return;

                await _client.ConnectAsync();
               
                return;
            case CommandType.Exit:
                Debug.Assert(command.Args is not null);                 
                Environment.Exit((int)command.Args[0]);
                return;
        }
    }

    public async Task<string?> ReadAsync()
    {
        while (true)
        {
            Console.Write("procmon-cli>");

            var command = await BuildCommandToken();

            if (command is null) continue;

            await RunCommand(command);
        }    
    }
}
