using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Client.Input.Transpiling;

namespace ProcessMonitor.Shared.Client.Input.Interpretation;

public sealed class CommandInterpretersCollection
{
    private Dictionary<CommandOperationType, Func<ClientApplicationState, CommandOperation, Task<Exception?>>> _interpreters;

    public CommandInterpretersCollection()
    {
        _interpreters = new Dictionary<CommandOperationType, Func<ClientApplicationState, CommandOperation, Task<Exception?>>>()
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

    private async Task<Exception?> InterpretConnectCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        if (!applicationState.Backend.IsRunning) return new InvalidOperationException("No server process instance was created");

        if (!applicationState.CommandsPipe.IsConnected())
        {
            var connectionException = await applicationState.CommandsPipe.TryConnectAsync(applicationState.CancellationToken);

            if (connectionException is not null) return connectionException;
        }

        if (!applicationState.TelemetryPipe.IsConnected())
        {
            var connectionException = await applicationState.TelemetryPipe.TryConnectAsync(applicationState.CancellationToken);

            if (connectionException is not null) 
            {
                await applicationState.Backend.DisposeAsync();
                await applicationState.CommandsPipe.DeinitializeAsync();
                return connectionException;
            }
        }

        if (!applicationState.CommandsPipe.IsConnected())
        {
            await applicationState.Backend.DisposeAsync();
            await applicationState.CommandsPipe.DeinitializeAsync();
            await applicationState.TelemetryPipe.DeinitializeAsync();
            return new Exception("Failed to connected to the 'Commands' pipe");
        }

        if (!applicationState.TelemetryPipe.IsConnected())
        {
            await applicationState.Backend.DisposeAsync();
            await applicationState.CommandsPipe.DeinitializeAsync();
            await applicationState.TelemetryPipe.DeinitializeAsync();
            return new Exception("Failed to connected to the 'Telemetry' pipe");
        }

        _ = Task.Run(async () => 
        {
            (var envelope, var envelopeReadingException) = await applicationState.TelemetryPipe.TryReadAsync<ProcessMetricsSnapshot>(applicationState.CancellationToken);

            if (envelopeReadingException is not null)
            {
                await applicationState.Backend.KillAsync();
                await applicationState.CommandsPipe.DeinitializeAsync();
                await applicationState.TelemetryPipe.DeinitializeAsync();
                return;
            }

            applicationState.LatestSnapshot = envelope.Payload;

        }, applicationState.CancellationToken);

        return null;
    }

    private async Task<Exception?> InterpretCreateCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        if (applicationState.Backend.IsRunning) return null;
            
        var backendCreationException = applicationState.Backend.TryCreate();

        if (backendCreationException is not null) return backendCreationException;

        var commandsInitException = applicationState.CommandsPipe.TryInitialize();

        if (commandsInitException is not null)
        {
            await applicationState.Backend.DisposeAsync();
            await applicationState.CommandsPipe.DeinitializeAsync();
            return commandsInitException;
        }

        var telemetryInitException = applicationState.TelemetryPipe.TryInitialize();

        if (telemetryInitException is not null) 
        {
            await applicationState.Backend.DisposeAsync();
            await applicationState.CommandsPipe.DeinitializeAsync();
            await applicationState.TelemetryPipe.DeinitializeAsync();
            return telemetryInitException;
        }

        return null;
    }

    private async Task<Exception?> InterpretDeleteCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        if (applicationState.Backend is not null)
        {
            await applicationState.Backend.DisposeAsync();
        }

        if (applicationState.CommandsPipe.IsConnected())
        {
            await applicationState.CommandsPipe.DeinitializeAsync();
        }

        if (applicationState.TelemetryPipe.IsConnected())
        {
            await applicationState.TelemetryPipe.DeinitializeAsync();
        }

        return null;
    }

    private async Task<Exception?> InterpretExitCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        await InterpretDeleteCommand(applicationState, op);

        Debug.Assert(op.Argument is not null && op.Argument is int, "All argument validation should've been completed at transpiling step.");

        Environment.Exit((int)op.Argument);

        return null;
    }

    private Task<Exception?> InterpretGetCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        var processIdStatus = applicationState.Configuration.ProcessId is null ? "undefined" : (applicationState.Configuration.ProcessId.ToString() ?? "null");
        var serverLocStatus = applicationState.Backend is null ? "undefined" : applicationState.Backend.Path;

        applicationState.Out.Append
        (
            $"""
            ProcessId: {processIdStatus}
            ServerLoc: {serverLocStatus}
            """
        );

        return Task.FromResult<Exception?>(null);
    }

    private Task<Exception?> InterpretHelpCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        applicationState.Out.Append
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

    private Task<Exception?> InterpretNoneCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        return Task.FromResult<Exception?>(null);
    }

    private Task<Exception?> InterpretSetCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        Debug.Assert(op.Argument is not null && op.Argument is int, "All argument validation should've been completed at transpiling step.");

        var processId = (int)op.Argument;

        applicationState.Configuration.ProcessId = processId;

        return Task.FromResult<Exception?>(null);
    }

    private Task<Exception?> InterpretStatusCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        var backendStatus = applicationState.Backend.HasExited 
            ? "exited" 
            : (applicationState.Backend.IsRunning 
                ? "running"
                : "not-running");

        var commandsPipeStatus = applicationState.CommandsPipe.IsConnected() 
            ? "connected" : "not-connected";

        var telemetryPipeStatus = applicationState.TelemetryPipe.IsConnected() 
            ? "connected" : "not-connected";

        applicationState.Out.Append
        (
            $"""
            Backend:       {backendStatus}
            CommandsPipe:  {commandsPipeStatus}
            TelemetryPipe: {telemetryPipeStatus}
            """
        );
        return Task.FromResult<Exception?>(null);
    }

    private async Task<Exception?> InterpretSendRequestCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        if (!applicationState.Backend.IsRunning) return null;

        if (!applicationState.CommandsPipe.IsConnected()) 
            return new InvalidOperationException("No connection via 'Commands' pipe was established");

        Debug.Assert(op.Argument is not null, "All argument validation should've been completed at transpiling step.");

        return await Task.Run(async() =>
        {
            var writingException = await applicationState.CommandsPipe.TryWriteAsync((MessageEnvelope<CommandRequest>)op.Argument, applicationState.CancellationToken);

            if (writingException is not null) return writingException;

            (var response, var readingException) = await applicationState.CommandsPipe.TryReadAsync<CommandResponse>(applicationState.CancellationToken);

            if (readingException is not null) return readingException;

            applicationState.Out.Append(response.Payload.Message);

            return null;
        }, applicationState.CancellationToken);
    }

    private Task<Exception?> InterpretUnknownCommand(ClientApplicationState applicationState, CommandOperation op)
    {
        return Task.FromResult<Exception?>(new ArgumentException("Unknown command"));
    }

    public async Task<Exception?> RunAsync(ClientApplicationState applicationState, CommandOperation op)
    {
        if (!_interpreters.TryGetValue(op.Type, out Func<ClientApplicationState, CommandOperation, Task<Exception?>>? interpreter)) 
            return await InterpretUnknownCommand(applicationState, op);

        return await interpreter(applicationState, op);
    }
}