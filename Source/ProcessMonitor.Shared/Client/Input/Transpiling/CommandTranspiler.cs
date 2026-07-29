using System;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;

using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Client.Input.Lexing;

namespace ProcessMonitor.Shared.Client.Input.Transpiling;

// TODO: Introduce a state class to hold references for metadata such as:
//    request id - which is assigned uniquely per each request operation
//    process id - which is going to be used by the 'start' command handler
//                 to find a reference of the process id if it were not
//                 speicified previously. For instance, when running the
//                 following instruction sequence: 'set <id> start'. 
//                 In this case the 'start' command should infer the 
//                 process id from some sort of a state holder and inject
//                 it as an argument while generating a list of ops
public sealed class CommandDispatchersCollection(List<CommandOperation> operations)
{
    private uint _requestId = 0;

    public List<CommandOperation> Operations { get; set; } = operations;

    public (int, Exception?) DispatchSetCommand(IReadOnlyList<TokenSlice> tokens, int cursor)
    {
        if (cursor < 0 || cursor > tokens.Count) return (0, new UnreachableException("Index out of range"));

        if (cursor == tokens.Count) cursor--;

        try
        {
            var slice = tokens[cursor];

            if (slice.Type != TokenSliceType.NumberLiteral) return (cursor + 1, new FormatException("'set' command expects a numeric literal argument"));

            var processId = int.Parse(slice.AsSpan());

            var op = new CommandOperation(CommandOperationType.SetProcessId, slice, processId);

            Operations.Add(op);

            return (cursor + 1, null);
        } 
        catch (Exception ex)
        {
            return (cursor + 1, ex);
        }
    }

    public (int, Exception?) DispatchExitCommand(IReadOnlyList<TokenSlice> tokens, int cursor)
    {
        if (cursor < 0 || cursor > tokens.Count) return (0, new UnreachableException("Index out of range"));

        TokenSlice slice;

        if (cursor == tokens.Count)
        {
            try
            {
                slice = tokens[cursor - 1];
                Operations.Add(new CommandOperation(CommandOperationType.Exit, slice, 0));
                return (cursor + 1, null);
            }
            catch (Exception ex)
            {
                return (cursor + 1, ex);
            }
        }

        try
        {
            slice = tokens[cursor];

            if (slice.Type != TokenSliceType.NumberLiteral) return (cursor + 1, new FormatException("Expected a numeric literal"));

            int exitCode = int.Parse(slice.AsSpan());

            Operations.Add(new CommandOperation(CommandOperationType.Exit, slice, exitCode));

            return (cursor + 1, null);
        }
        catch (Exception ex)
        {
            return (cursor + 1, ex);
        }
    }

    public (int, Exception?) DispatchStartCommand(IReadOnlyList<TokenSlice> tokens, int cursor)
    {
        if (cursor < 0 || cursor > tokens.Count) return (0, new UnreachableException("Index out of range"));

        if (cursor == tokens.Count)
        {
            try
            {
                var slice = tokens[cursor - 1];

                Operations.Add(new CommandOperation(CommandOperationType.CreateBackendProcess, slice, null));
                Operations.Add(new CommandOperation(CommandOperationType.ConnectToBackendProcess, slice, null));

                return (cursor + 1, AddSendRequestOperation(slice, "post", "monitoring", new { version = 0.1, requestId = _requestId, pid = 0 }));
                
            } 
            catch (Exception ex)
            {
                return (cursor + 1, ex);
            }
        }

        try
        {
            var slice = tokens[cursor];

            if (slice.Type != TokenSliceType.NumberLiteral)
            {
                Operations.Add(new CommandOperation(CommandOperationType.CreateBackendProcess, slice, null));
                Operations.Add(new CommandOperation(CommandOperationType.ConnectToBackendProcess, slice, null));

                return (cursor + 1, AddSendRequestOperation(slice, "post", "monitoring", new { version = 0.1, requestId = _requestId, pid = 0 }));
            }
        }
        catch (Exception ex)
        {
            return (cursor + 1, ex);
        }

        try
        {
            var slice = tokens[cursor];
            int processId = int.Parse(slice.AsSpan());

            Operations.Add(new CommandOperation(CommandOperationType.SetProcessId, slice, processId));
            Operations.Add(new CommandOperation(CommandOperationType.CreateBackendProcess, slice, null));
            Operations.Add(new CommandOperation(CommandOperationType.ConnectToBackendProcess, slice, null));

            return (cursor + 1, AddSendRequestOperation(slice, "post", "monitoring", new { version = 0.1, requestId = _requestId, pid = processId }));
        }
        catch (Exception ex)
        {
            return (cursor + 1, ex);
        }
    }

    public (int, Exception?) DispatchStopCommand(IReadOnlyList<TokenSlice> tokens, int cursor)
    {
        if (cursor < 0 || cursor > tokens.Count) return (0, new UnreachableException("Index out of range"));

        if (cursor == tokens.Count) cursor--;

        TokenSlice slice;

        try
        {
            slice = tokens[cursor];
        }
        catch (Exception ex)
        {
            return (cursor + 1, ex);
        }

        var operationAppendException = AddSendRequestOperation(slice, "delete", "monitoring", new { version = 0.1, requestId = _requestId });

        return (cursor + 1, operationAppendException);
    }

    private Exception? AddSendRequestOperation(TokenSlice sourceToken, string method, string route, object rawBody)
    {
        JsonElement body;

        try
        {
            body = JsonSerializer.SerializeToElement(rawBody);
        }
        catch (Exception ex)
        {
            return ex;
        }

        var envelope = new MessageEnvelope<CommandRequest>
        {
            Type = MessageType.CommandRequest,
            Payload = new CommandRequest()
            {
                Method = method,
                Route = route,
                Body = body
            }
        };

        Operations.Add(new CommandOperation(CommandOperationType.SendRequest, sourceToken, envelope));

        _requestId++;

        return null;
    }

    public (int, Exception?) DispatchNoArgumentCommand(IReadOnlyList<TokenSlice> tokens, int cursor, CommandOperationType op)
    {
        if (cursor < 0 || cursor > tokens.Count) return (0, new UnreachableException("Index out of range"));

        if (cursor == tokens.Count) cursor--;

        try
        {
            var slice = tokens[cursor];
            Operations.Add(new CommandOperation(op, slice, null));
            return (cursor + 1, null);
        }
        catch (Exception ex)
        {
            return (cursor + 1, ex);
        }
    }

    public (int, Exception?) DispatchUnknownCommand(IReadOnlyList<TokenSlice> tokens, int cursor)
    {
        return (cursor + 1, new InvalidOperationException($"Command name was not recognized"));
    }

    public (int, Exception?) DispatchGetCommand(IReadOnlyList<TokenSlice> tokens, int cursor) 
        => DispatchNoArgumentCommand(tokens, cursor, CommandOperationType.PrintRuntimeConfig);

    public (int, Exception?) DispatchHelpCommand(IReadOnlyList<TokenSlice> tokens, int cursor) 
        => DispatchNoArgumentCommand(tokens, cursor, CommandOperationType.PrintHelp);

    public (int, Exception?) DispatchCreateCommand(IReadOnlyList<TokenSlice> tokens, int cursor) 
        => DispatchNoArgumentCommand(tokens, cursor, CommandOperationType.CreateBackendProcess);

    public (int, Exception?) DispatchDeleteCommand(IReadOnlyList<TokenSlice> tokens, int cursor) 
        => DispatchNoArgumentCommand(tokens, cursor, CommandOperationType.KillBackendProcess);

    public (int, Exception?) DispatchStatusCommand(IReadOnlyList<TokenSlice> tokens, int cursor) 
        => DispatchNoArgumentCommand(tokens, cursor, CommandOperationType.PrintStatus);

    public (int, Exception?) DispatchConnectCommand(IReadOnlyList<TokenSlice> tokens, int cursor) 
        => DispatchNoArgumentCommand(tokens, cursor, CommandOperationType.ConnectToBackendProcess);
} 

public sealed class CommandTranspiler
{
    public List<CommandOperation> Operations { get; set; } = new(32);

    private readonly Dictionary<Commands, Func<IReadOnlyList<TokenSlice>, int, (int, Exception?)>> _dispatchers;

    private readonly CommandDispatchersCollection _dispatchersCollection;

    public CommandTranspiler()
    {
        _dispatchersCollection = new CommandDispatchersCollection(Operations);

        _dispatchers = new Dictionary<Commands, Func<IReadOnlyList<TokenSlice>, int, (int, Exception?)>>()
        {
            [Commands.Set] = _dispatchersCollection.DispatchSetCommand,
            [Commands.Get] = _dispatchersCollection.DispatchGetCommand,  
            [Commands.Help] = _dispatchersCollection.DispatchHelpCommand,  
            [Commands.Stop] = _dispatchersCollection.DispatchStopCommand,  
            [Commands.Exit] = _dispatchersCollection.DispatchExitCommand,
            [Commands.Start] = _dispatchersCollection.DispatchStartCommand,
            [Commands.Create] = _dispatchersCollection.DispatchCreateCommand,  
            [Commands.Delete] = _dispatchersCollection.DispatchDeleteCommand,  
            [Commands.Status] = _dispatchersCollection.DispatchStatusCommand,  
            [Commands.Connect] = _dispatchersCollection.DispatchConnectCommand,  
            [Commands.Unknown] = _dispatchersCollection.DispatchUnknownCommand,  
        };
    }

    private Exception? DispatchToken(IReadOnlyList<TokenSlice> tokens, ref int cursor)
    {
        if (cursor < 0) return new UnreachableException("Index is less then 0");

        if (cursor >= tokens.Count) return null;

        var token = tokens[cursor]; cursor++;

        var dispatcher = _dispatchers[token.AsSpan().AsCommand()];

        (int newCursorPosition, Exception? result) = dispatcher(tokens, cursor);

        cursor = newCursorPosition;

        return result;
    }

    public Exception? Transpile(IReadOnlyList<TokenSlice> tokens, bool clear = true)
    {
        if (clear) Operations.Clear();

        int cursor = 0;

        if (tokens.Count == 0) return null;

        while (cursor < tokens.Count)
        {
            var dispatchingException = DispatchToken(tokens, ref cursor);

            if (dispatchingException is not null) return dispatchingException;
        }

        return null;
    }
}