using System;
using System.Text.Json;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Options;

using ProcessMonitor.CLI.State;
using ProcessMonitor.CLI.Common;
using ProcessMonitor.CLI.Transport;

using ProcessMonitor.Shared.Protocol;

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
    Status,
    Connect,
    Start,
    Stop,
    Get,
    Length,
}

// TODO (not urgent): Move this type definition to a separate file
public sealed class CommandToken(CommandType operation = CommandType.None, object[]? args = null)
{
    public CommandType Operation { get; set; } = operation;

    public object[]? Args { get; set; } = args;
}

public sealed class ConsoleInputReader
{
    private readonly BackendProcess _backend;
    private readonly CommandPipeClient _commandsPipeClient;
    private readonly TelemetryPipeClient _telemetryPipeClient;

    private uint _requestId = 0;

    private readonly Dictionary<string, CommandType> _map;

    private readonly Dictionary<CommandType, Func<string[], bool>> _parsers;

    private IOptions<RuntimeState> _state;

    private List<CommandToken> _tokens;

    public ConsoleInputReader(
        BackendProcess backend, 
        CommandPipeClient commandPipeClient,
        TelemetryPipeClient telemetryPipeClient,
        IOptions<RuntimeState> options)
    {
        _backend = backend;
        _commandsPipeClient = commandPipeClient;
        _telemetryPipeClient = telemetryPipeClient;
        _state = options;

        _map = new Dictionary<string, CommandType>()
        {
            ["help"] = CommandType.Help,
            ["h"] = CommandType.Help,
            ["create"] = CommandType.Create,
            ["exit"] = CommandType.Exit,
            ["q"] = CommandType.Exit,
            ["delete"] = CommandType.Delete,
            ["del"] = CommandType.Delete,
            ["set"] = CommandType.Set,
            ["status"] = CommandType.Status,
            ["stat"] = CommandType.Status,
            ["connect"] = CommandType.Connect,
            ["start"] = CommandType.Start,
            ["stop"] = CommandType.Stop,
            ["get"] = CommandType.Get,
        };

        _parsers = new Dictionary<CommandType, Func<string[], bool>>()
        {
            [CommandType.Start] = ParseStartCommand,
            [CommandType.Exit] = ParseExitCommand,
            [CommandType.Set] = ParseSetCommand,
        };

        _tokens = new List<CommandToken>();

        _backend.AddOnExitHandler(async (sender, e) =>
        {
            Console.WriteLine("procmon: warning: the server process has exited. Run 'create' & 'connect' commands to reconnect again.");
            await _backend.DisposeAsync();
            await _commandsPipeClient.CleanupConnection();
            await _telemetryPipeClient.CleanupConnectionAsync();
        });
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

    private bool ParseStartCommand(string[] words)
    {
        if (words.Length == 1)
        {
            // TODO: make target pid nullable to check whether it's been initialized
            // since checking it to zero is unreliable (although usually working)
            if (_state.Value.TargetPid == 0)
            {
                Console.WriteLine("procmon: error: Failed to execute the 'start' command: no target process id was specified.");
                Console.WriteLine("procmon: note: run the 'set <pid>' command or provide the '--pid' flag in the input arguments.");
                return false;
            }

            _tokens.Add(new CommandToken(CommandType.Create));
            _tokens.Add(new CommandToken(CommandType.Connect));
            _tokens.Add(new CommandToken(CommandType.Set, [_state.Value.TargetPid]));
            _tokens.Add(new CommandToken(CommandType.Start));

            return true;
        }

        if (!int.TryParse(words[1], out int pid))
        {
            Console.WriteLine($"procmon: error: Could not convert `{words[1]}` to an integer.");
            return false;
        }

        if (pid == _state.Value.TargetPid) return true;

        _state.Value.TargetPid = pid;

        _tokens.Add(new CommandToken(CommandType.Create));
        _tokens.Add(new CommandToken(CommandType.Connect));
        _tokens.Add(new CommandToken(CommandType.Set, [_state.Value.TargetPid]));
        _tokens.Add(new CommandToken(CommandType.Start));

        return true;
    }

    private bool ParseExitCommand(string[] words)
    {
        var exitCode = 0;

        if (words.Length > 1)  
        {
            if (!int.TryParse(words[1], out exitCode))
            {
                Console.WriteLine($"procmon: error: Could not convert `{words[1]}` to an integer.");
                return false;
            }
        }
            
        _tokens.Add(new CommandToken(CommandType.Delete));
        _tokens.Add(new CommandToken(CommandType.Exit, [exitCode]));

        return true;
    }

    private bool ParseSetCommand(string[] words)
    {
        if (words.Length == 1)  
        {
            Console.WriteLine("procmon: error: Missing an argument for the 'set' command.");
            Console.WriteLine("procmon: note: set <pid>");
            return false;   
        }

        if (!int.TryParse(words[1], out int pid))  
        {
            Console.WriteLine($"procmon: error: Could not convert '{words[1]}' to an integer.");
            return false;   
        }

        if (pid == _state.Value.TargetPid) return true;
            
        _tokens.Add(new CommandToken(CommandType.Set, [pid]));

        return true;
    }

    // TODO (not urgent): Replace the switch case with a dicitionary mapping
    private async Task<bool> TryAddCommandTokens(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            Console.WriteLine("procmon: info: Could not parse a command: cancellation requested.");
            return false;    
        }

        var words = await LexInput(ct);

        if (words is null) return false;

        if (!_map.TryGetValue(words[0], out CommandType commandType))
        {
            Console.WriteLine($"procmon: error: The `{words[0]}` command was not recognized.\n\tRun 'h' or 'help' to get all the commands list.");
            return false;
        }

        if (!_parsers.TryGetValue(commandType, out Func<string[], bool>? handler))
        {
            Debug.Assert((int)commandType < (int)CommandType.Length, "The condition assumes that all the incorrect types were filtered by the previous condition.");
            _tokens.Add(new CommandToken(commandType));
            return true;
        }

        return handler(words);
    }

    // Note: Consider merging all the statements into a single one to reduce uncessary IO calls
    private static void PrintUsage()
    {
        Console.WriteLine("`procmon-cli` is a console client that lets you communicate with the metrics server.");
        Console.WriteLine("To interact with the api, use a predefined set of commands listed below:\n");
        Console.WriteLine("\thelp|h       - get this usage message.");
        Console.WriteLine();
        Console.WriteLine("\tcreate       - start up the ProcessMonitor.Backend server process.");
        Console.WriteLine("\tconnect      - connects to the 'Telemetry' and 'Commands' pipes.");
        Console.WriteLine("\tdelete       - kill the ProcessMonitor.Backend server process.");
        Console.WriteLine("\tset <int>    - requests the server to update the process id.");
        Console.WriteLine("\tstatus|stat  - shows the current connection stats such as:");
        Console.WriteLine("\t                  1. Is the backend process running");
        Console.WriteLine("\t                  2. Is the client connected to the backend (via the 'Commands' pipe)");
        Console.WriteLine();
        Console.WriteLine("\texit <code?> - exit the client process with the `<code>` exit status.");
        Console.WriteLine("\t               if provided. Otherwise exit with `0`.");
        Console.WriteLine("\tq            - exit with `0` exit code.");
    }

    private void PrintStatus()
    {
        string backendStatus;

        if (_backend.HasExited)
        {
            backendStatus = "Exited";
        }
        else
        {
            backendStatus = _backend.IsRunning ? "Running" : "Not running";
        }

        string commandsPipeStatus = _commandsPipeClient.IsConnected ? "Connected" : "Not connected";
        string telemetryPipeStatus = _telemetryPipeClient.IsConnected ? "Connected" : "Not connected";

        Console.WriteLine($"Backend: {backendStatus}\nCommands Pipe: {commandsPipeStatus}\nTelemetry Pipe: {telemetryPipeStatus}");
    }

    // Note: this method assumes the input arguments are
    // valid since they're handled by the BuildCommandToken method
    // TODO (not urgent): Replace the switch statement with a dictionary mapping 
    private async Task RunCommand(CommandToken command, CancellationToken ct)
    {
        switch (command.Operation)
        {
            case CommandType.None:
                return;
            case CommandType.Help:
                PrintUsage();
                return;
            case CommandType.Get:
                var pidInfo = _state.Value.TargetPid == 0 ? "not defined" : _state.Value.TargetPid.ToString();
                Console.WriteLine($"pid: {pidInfo}\nserver file path: {_state.Value.BackendProcessFilePath}");
                return;
            case CommandType.Connect:
                if (!_backend.IsRunning)
                {
                    Console.WriteLine("procmon-cli: error: Could not execute the 'connect' command: no server process has been created.");
                    Console.WriteLine("procmon-cli: note: Consider running the 'create' command first before attempting to connect.");
                    return;
                }

                if (!_commandsPipeClient.IsConnected)
                {
                    if (!await _commandsPipeClient.TryConnectAsync(ct))
                    {
                        Console.WriteLine("procmon-cli: error: Could not execute the 'connect' command: Failed to connect to the 'Commands' pipe.");
                        await _backend.KillAsync();
                        return;
                    }
                }
                
                if (!_telemetryPipeClient.IsConnected)
                {
                    if (!await _telemetryPipeClient.TryConnectAsync(ct))
                    {
                        Console.WriteLine("procmon-cli: error: Could not execute the 'connect' command: Failed to connect to the 'Telemetry' pipe.");
                        await _backend.KillAsync();
                        await _commandsPipeClient.CleanupConnection();
                        return;
                    }
                    
                    _ = Task.Run(async () => 
                    {
                        try
                        {
                            await _telemetryPipeClient.ReadAsync(ct);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"procmon-cli: error: The 'Telemetry' pipe handling exited abnormally: {ex.Message}");
                            await _backend.KillAsync();
                            await _telemetryPipeClient.CleanupConnectionAsync();
                            await _commandsPipeClient.CleanupConnection();
                        }
                    }, ct);
                }

                return;
            case CommandType.Create:
                if (_backend.IsRunning) return;

                if (!_backend.Create())
                {
                    Console.WriteLine($"procmon: error: Could not startup a server process: {_backend.GetErrorString()}");
                }
                return;
            case CommandType.Status:
                PrintStatus();
                return;
            case CommandType.Exit:
                Debug.Assert(command.Args is not null);             
                Environment.Exit((int)command.Args[0]);
                return;
            case CommandType.Delete:
                await _backend.DisposeAsync();
                await _commandsPipeClient.CleanupConnection();
                await _telemetryPipeClient.CleanupConnectionAsync();
                return;
            case CommandType.Set:
                Debug.Assert(command.Args is not null);             
                _state.Value.TargetPid = (int)command.Args[0];
                return;
            case CommandType.Stop:
            {
                if (!_backend.IsRunning || !_telemetryPipeClient.IsConnected) return;

                var body = new
                {
                    version = 1,
                    requestId = _requestId,
                };

                var bodyElement = JsonSerializer.SerializeToElement(body);

                var envelope = new MessageEnvelope<CommandRequest>
                {
                    Type = MessageType.CommandRequest,
                    Payload = new CommandRequest
                    {
                        Method = "delete",
                        Route = "monitoring",
                        Body = bodyElement
                    }    
                };
                var result = await _commandsPipeClient.WriteAsync(envelope, ct);

                if (!result)
                {
                    Console.WriteLine("procmon: error: Failed to execute the 'stop' command.");
                    return;    
                }

                _requestId++;

                return;
            }
            case CommandType.Start:
            {
                
                Debug.Assert(_telemetryPipeClient.IsConnected && _commandsPipeClient.IsConnected && _backend.IsRunning, 
                    "The 'start' command is expected to be split into 'create & connect & set' commands. Therefore if some of the variables above are invalid, it's a bug.");
                
                var body = new
                {
                    version = 1,
                    requestId = _requestId,
                    pid = _state.Value.TargetPid
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

                var result = await _commandsPipeClient.WriteAsync(envelope, ct);

                if (!result)
                {
                    Console.WriteLine("procmon: error: Failed to execute the 'start' command.");
                    return;    
                }

                _requestId++;

                return;
            }
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

            if (!await TryAddCommandTokens(ct)) continue;

            foreach (var token in _tokens)
            {
                await RunCommand(token, ct);
            }

            _tokens.Clear();
        }    
    }
}
