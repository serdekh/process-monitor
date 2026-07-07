using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using ProcessMonitor.CLI.Transport;
using ProcessMonitor.Shared.Protocol;
using System.Text.Json;

namespace ProcessMonitor.CLI.Input;

// TODO (not urgent): Move this type definition to a separate file
public enum CommandType
{
    None = 0,
    Help,
    Create,
    Exit,
    Delete,
    Set,
}

// TODO (not urgent): Move this type definition to a separate file
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

    private int? _pid = null;

    private uint _requestId = 0;

    private readonly Dictionary<string, CommandType> _map;

    public ConsoleInputReader(string backendFilePath)
    {
        _map = new Dictionary<string, CommandType>()
        {
            ["help"] = CommandType.Help,
            ["h"] = CommandType.Help,
            ["create"] = CommandType.Create,
            ["exit"] = CommandType.Exit,
            ["q"] = CommandType.Exit,
            ["delete"] = CommandType.Delete,
            ["set"] = CommandType.Set
        };
  
        _client = new CommandPipeClient(backendFilePath);
    }

    // TODO (not urgent): Replace console logging with the Microsoft logger.
    // TODO (not urgent): Make the lexer to support parsing things like
    //     several commands: 'c1 & c2'
    //     transactions:     '{ create & set }'
    //     conditions:       '{ create } if exit 1 end'
    private static async Task<string[]?> LexInput(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            Console.WriteLine("procmon: Could not parse input string: cancellation requested.");
            return null;
        }

        string? input = await Task.Run(() => Console.ReadLine(), ct);

        if (string.IsNullOrWhiteSpace(input)) return null;

        string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries |
                                          StringSplitOptions.TrimEntries);
        return words;
    }

    // TODO (not urgent): Replace the switch case with a dicitionary mapping
    private async Task<Command?> BuildCommandToken(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            Console.WriteLine("procmon: info: Could not parse a command: cancellation requested.");
            return null;    
        }

        var tokens = await LexInput(ct);

        if (tokens is null) return null;

        if (!_map.TryGetValue(tokens[0], out CommandType commandType))
        {
            Console.WriteLine($"procmon: error: The `{tokens[0]}` command was not recognized.\n\tRun 'h' or 'help' to get all the commands list.");
            return null;
        }

        var command = new Command(operation: commandType, args: null);

        switch (commandType)
        {
            case CommandType.Help:
                return command;

            case CommandType.Create: 
                return command;

            case CommandType.Delete:
                return command;

            case CommandType.Exit:
                var exitCode = 0;

                if (tokens.Length > 1)  
                {
                    if (!int.TryParse(tokens[1], out exitCode))
                    {
                        Console.WriteLine($"procmon: error: Could not convert `{tokens[1]}` to an integer.");
                        return null;
                    }
                }
                    
                command.Args = [exitCode];
              
                return command;
            
            case CommandType.Set:
                if (tokens.Length == 1)
                {
                    Console.WriteLine("procmon: error: Missing an argument for the `set` command.\nprocmon: note: set <int>");
                    return null;
                }

                if (!int.TryParse(tokens[1], out int pid))
                {
                    Console.WriteLine($"procmon: error: Could not convert `{tokens[1]}` to an integer.");
                    return null;
                }

                if (pid == _pid) return null;

                _pid = pid;

                command.Args = [pid];

                return command;

            default: 
                return null;
        }
    }

    // Note: Consider merging all the statements into a single one to reduce uncessary IO calls
    private static void PrintUsage()
    {
        Console.WriteLine("`procmon-cli` is a console client that lets you communicate with the metrics server.");
        Console.WriteLine("To interact with the api, use a predefined set of commands listed below:\n");
        Console.WriteLine("\thelp|h       - get this usage message.");
        Console.WriteLine();
        Console.WriteLine("\tcreate       - start up the ProcessMonitor.Backend server process.");
        Console.WriteLine("\tdelete       - kill the ProcessMonitor.Backend server process.");
        Console.WriteLine("\tset <int>    - requests the server to update the process id.");
        Console.WriteLine();
        Console.WriteLine("\texit <code?> - exit the client process with the `<code>` exit status.");
        Console.WriteLine("\t               if provided. Otherwise exit with `0`.");
        Console.WriteLine("\tq            - exit with `0` exit code.");
    }

    // Note: this method assumes the input arguments are
    // valid since they're handled by the BuildCommandToken method
    // TODO (not urgent): Replace the switch statement with a dictionary mapping 
    private async Task RunCommand(Command command, CancellationToken ct)
    {
        switch (command.Operation)
        {
            case CommandType.None:
                return;
            case CommandType.Help:
                PrintUsage();
                return;
            case CommandType.Create:
                await _client.ConnectAsync(ct);
                return;
            case CommandType.Exit:
                Debug.Assert(command.Args is not null);                 
                Environment.Exit((int)command.Args[0]);
                return;
            case CommandType.Delete:
                await _client.CleanupConnection();
                return;
            case CommandType.Set:
                var body = new
                {
                    version = 1,
                    requestId = _requestId,
                    pid = _pid
                };

                var bodyElement = JsonSerializer.SerializeToElement(body);

                var envelope = new MessageEnvelope<CommandRequest>
                {
                    Type = MessageType.CommandRequest,
                    Payload = new CommandRequest
                    {
                        Method = "post",
                        Route = "monitoring",
                        Body = bodyElement
                    }    
                };

                var result = await _client.WriteAsync(envelope, ct);

                if (!result)
                {
                    Console.WriteLine("prcomon: error: Failed to execute the `set` command.");
                    return;    
                }

                _requestId++;

                return;
        }
    }

    // TODO (not urgent): replace custom logs with the Microsoft logger.
    public async Task ReadAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            Console.WriteLine("procmon: info: Could not start reading user input: cancellation requested.");
            return;
        }

        while (!ct.IsCancellationRequested)
        {
            Console.Write("procmon-cli>");

            var command = await BuildCommandToken(ct);

            if (command is null) continue;

            await RunCommand(command, ct);
        }    
    }
}
