using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Input.Transpiling;

namespace ProcessMonitor.CLI.Input.Interpretation;

public sealed class CommandInterpretersCollection
{
    private Dictionary<CommandOperationType, Func<CommandInterpreterState, CommandOperation, Task<Exception?>>> _interpreters;

    public CommandInterpretersCollection()
    {
        _interpreters = new Dictionary<CommandOperationType, Func<CommandInterpreterState, CommandOperation, Task<Exception?>>>()
        {
            [CommandOperationType.ConnectToBackendProcess] = InterpretConnectCommand,
            [CommandOperationType.CreateBackendProcess] = InterpretCreateCommand,
            [CommandOperationType.KillBackendProcess] = InterpretDeleteCommand,
            [CommandOperationType.Exit] = InterpretExitCommand,
            [CommandOperationType.PrintRuntimeConfig] = InterpretGetCommand,
            [CommandOperationType.PrintHelp] = InterpretHelpCommand,
            [CommandOperationType.Unknown] = InterpretNoneCommand,
            [CommandOperationType.SetProcessId] = InterpretSetCommand,
            [CommandOperationType.PrintStatus] = InterpretStatusCommand,
            [CommandOperationType.SendRequest] = InterpretSendRequestCommand,
        };
    }

    private async Task<Exception?> InterpretConnectCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        if (!interpreterState.Backend.IsRunning) return new InvalidOperationException("No server process instance was created");

        if (!interpreterState.CommandsPipe.IsConnected())
        {
            var connectionException = await interpreterState.CommandsPipe.TryConnectAsync(interpreterState.CancellationToken);

            if (connectionException is not null) return connectionException;
        }

        if (!interpreterState.TelemetryPipe.IsConnected())
        {
            var connectionException = await interpreterState.TelemetryPipe.TryConnectAsync(interpreterState.CancellationToken);

            if (connectionException is not null) 
            {
                await interpreterState.Backend.DisposeAsync();
                await interpreterState.CommandsPipe.DeinitializeAsync();
                return connectionException;
            }
        }

        if (!interpreterState.CommandsPipe.IsConnected())
        {
            await interpreterState.Backend.DisposeAsync();
            await interpreterState.CommandsPipe.DeinitializeAsync();
            await interpreterState.TelemetryPipe.DeinitializeAsync();
            return new Exception("Failed to connected to the 'Commands' pipe");
        }

        if (!interpreterState.TelemetryPipe.IsConnected())
        {
            await interpreterState.Backend.DisposeAsync();
            await interpreterState.CommandsPipe.DeinitializeAsync();
            await interpreterState.TelemetryPipe.DeinitializeAsync();
            return new Exception("Failed to connected to the 'Telemetry' pipe");
        }

        _ = Task.Run(async () => 
        {
            (var envelope, var envelopeReadingException) = await interpreterState.TelemetryPipe.TryReadAsync<ProcessMetricsSnapshot>(interpreterState.CancellationToken);

            if (envelopeReadingException is not null)
            {
                await interpreterState.Backend.KillAsync();
                await interpreterState.CommandsPipe.DeinitializeAsync();
                await interpreterState.TelemetryPipe.DeinitializeAsync();
                return;
            }

            interpreterState.Telemetry.Add(envelope.Payload);
        }, interpreterState.CancellationToken);

        return null;
    }

    private async Task<Exception?> InterpretCreateCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        if (interpreterState.Backend.IsRunning) return null;
            
        if (interpreterState.Backend.HasError) return interpreterState.Backend.Error;

        if (!interpreterState.Backend.Create()) return interpreterState.Backend.Error;

        var commandsInitException = interpreterState.CommandsPipe.TryInitialize();

        if (commandsInitException is not null)
        {
            await interpreterState.Backend.DisposeAsync();
            await interpreterState.CommandsPipe.DeinitializeAsync();
            return commandsInitException;
        }

        var telemetryInitException = interpreterState.TelemetryPipe.TryInitialize();

        if (telemetryInitException is not null) 
        {
            await interpreterState.Backend.DisposeAsync();
            await interpreterState.CommandsPipe.DeinitializeAsync();
            await interpreterState.TelemetryPipe.DeinitializeAsync();
            return telemetryInitException;
        }

        return null;
    }

    private async Task<Exception?> InterpretDeleteCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        if (interpreterState.Backend is not null)
        {
            await interpreterState.Backend.DisposeAsync();
        }

        if (interpreterState.CommandsPipe.IsConnected())
        {
            await interpreterState.CommandsPipe.DeinitializeAsync();
        }

        if (interpreterState.TelemetryPipe.IsConnected())
        {
            await interpreterState.TelemetryPipe.DeinitializeAsync();
        }

        return null;
    }

    private async Task<Exception?> InterpretExitCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        await InterpretDeleteCommand(interpreterState, op);

        Debug.Assert(op.Argument is not null && op.Argument is int, "All argument validation should've been completed at transpiling step.");

        Environment.Exit((int)op.Argument);

        return null;
    }

    private Task<Exception?> InterpretGetCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        var processIdStatus = interpreterState.ProcessId is null ? "undefined" : (interpreterState.ProcessId.ToString() ?? "null");
        var serverLocStatus = interpreterState.Backend is null ? "undefined" : interpreterState.Backend.Path;

        interpreterState.Out.Append
        (
            $"""
            ProcessId: {processIdStatus}
            ServerLoc: {serverLocStatus}
            """
        );

        return Task.FromResult<Exception?>(null);
    }

    private Task<Exception?> InterpretHelpCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        interpreterState.Out.Append
        (
            """
        
            Procmon is a cli tool for interacting with the ProcessMonitor application server.
            It provides a set of commands you can use to call to the server api. 
            Here is a complete list of all the supported commands:

                connect      - establishes a connection between the cli client and the server.
                               Requires an instance of the server process. Otherwise returns
                               an error message and does nothing. Upon success, connects to both
                               the 'Commands' and 'Telemetry' named pipes.

                create       - creates an instance of the server program upon success. In case of 
                               failure, an error message is displayed and nothing is performed.

                delete|del   - kills the active server process if there was any. 

                exit <code?> - completely exits the application and cleans up all the allocated
                               resources, namely: connection pipes (closing both the 'Telemetry'
                               and the 'Commands' pipes) and the server process which gets killed 
                               automatically. After that, the application exits with the optional
                               exit status if provided. Otherwise, it exits with '0'. 

                q            - does the same thing as the 'exit' command but without the optional
                               exit code specifier. It is always set to '0'.

                get          - prints the execution state of the program, such as:
                                    ProcessId: |undefined|<int>   |
                                    ServerLoc: |undefined|<string>|.

                help|h       - prints this message.

                set <int>    - initializes the 'ProcessId' value of the execution state. The
                               argument is required, otherwise an error message is printed and
                               nothing is perfomed. By default, the value is undefined.

                start <int?> - combines logic of 'set', 'create', and 'connect' commands into
                               a single command. An optional argument can be provided. If it
                               is not specified, the interpreter will try to look up the
                               'ProcessId' value in its execution state. If both the execution
                               state and the argument are provided, the argument takes presendence
                               and the previous execution state value is not modified.

                status|stat  - prints execution state related to the inter-process connection such as:
                                   Backend:       |running  |not-running  |exited|
                                   CommandsPipe:  |connected|not-connected|      |
                                   TelemetryPipe: |connected|not-connected|      |.

                stop         - sends a request to the server process to reset its process id reference
                               and to stop sending further metrics. *Currently this command has bugs
                               since it collides with other text written in stdout and the response
                               handling is not implemented. Sorry for that.

            """
        );
        return Task.FromResult<Exception?>(null);
    }

    private Task<Exception?> InterpretNoneCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        return Task.FromResult<Exception?>(null);
    }

    private Task<Exception?> InterpretSetCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        Debug.Assert(op.Argument is not null && op.Argument is int, "All argument validation should've been completed at transpiling step.");

        var processId = (int)op.Argument;

        interpreterState.ProcessId = processId;

        return Task.FromResult<Exception?>(null);
    }

    private Task<Exception?> InterpretStatusCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        var backendStatus = interpreterState.Backend.HasExited 
            ? "exited" 
            : (interpreterState.Backend.IsRunning 
                ? "running"
                : "not-running");

        var commandsPipeStatus = interpreterState.CommandsPipe.IsConnected() 
            ? "connected" : "not-connected";

        var telemetryPipeStatus = interpreterState.TelemetryPipe.IsConnected() 
            ? "connected" : "not-connected";

        interpreterState.Out.Append
        (
            $"""
            Backend:       {backendStatus}
            CommandsPipe:  {commandsPipeStatus}
            TelemetryPipe: {telemetryPipeStatus}
            """
        );
        return Task.FromResult<Exception?>(null);
    }

    private async Task<Exception?> InterpretSendRequestCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        if (!interpreterState.Backend.IsRunning) return null;

        if (!interpreterState.CommandsPipe.IsConnected()) 
            return new InvalidOperationException("No connection via 'Commands' pipe was established");

        Debug.Assert(op.Argument is not null, "All argument validation should've been completed at transpiling step.");

        var writingException = await interpreterState.CommandsPipe.TryWriteAsync((MessageEnvelope<CommandRequest>)op.Argument, interpreterState.CancellationToken);

        return writingException;
    }

    private Task<Exception?> InterpretUnknownCommand(CommandInterpreterState interpreterState, CommandOperation op)
    {
        return Task.FromResult<Exception?>(new ArgumentException("Unknown command"));
    }

    public async Task<Exception?> RunAsync(CommandInterpreterState interpreterState, CommandOperation op)
    {
        if (!_interpreters.TryGetValue(op.Type, out Func<CommandInterpreterState, CommandOperation, Task<Exception?>>? interpreter)) 
            return await InterpretUnknownCommand(interpreterState, op);

        return await interpreter(interpreterState, op);
    }
}