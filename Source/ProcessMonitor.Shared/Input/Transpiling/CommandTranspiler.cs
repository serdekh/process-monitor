using System;
using System.Text.Json;
using System.Diagnostics;
using System.Collections.Generic;

using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Input.Lexing;

namespace ProcessMonitor.Shared.Input.Transpiling;

public sealed class CommandDispatchersCollection(List<CommandOperation> operations)
{
    public List<CommandOperation> Operations { get; set; } = operations;

    public (int, Exception?) DispatchSetCommand(IReadOnlyList<TokenSlice> tokens, int cursor)
    {
        if (cursor < 0 || cursor > tokens.Count) return (0, new UnreachableException("Index out of range"));

        if (cursor == tokens.Count) cursor--;

        try
        {
            var slice = tokens[cursor];

            if (slice.Type != TokenSliceType.NumberLiteral) return (cursor + 1, new FormatException("'set' command expects a numeric literal argument"));

            var processId = int.Parse(slice.Source.AsSpan(slice.StartIndex, slice.Length));

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

            int exitCode = int.Parse(slice.Source.AsSpan(slice.StartIndex, slice.Length));

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

                return (cursor + 1, null);
                
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

                return (cursor + 1, null);
            }
        }
        catch (Exception ex)
        {
            return (cursor + 1, ex);
        }

        try
        {
            var slice = tokens[cursor];
            int processId = int.Parse(slice.Source.AsSpan(slice.StartIndex, slice.Length));

            Operations.Add(new CommandOperation(CommandOperationType.SetProcessId, slice, processId));
            Operations.Add(new CommandOperation(CommandOperationType.CreateBackendProcess, slice, null));
            Operations.Add(new CommandOperation(CommandOperationType.ConnectToBackendProcess, slice, null));

            return (cursor + 1, null);
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

        var body = new
        {
            version = 0.1,
        };

        JsonElement bodyElement;

        try
        {
            bodyElement = JsonSerializer.SerializeToElement(body);
        }
        catch (Exception ex)
        {
            return (cursor + 1, ex);
        }

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

        Operations.Add(new CommandOperation(CommandOperationType.SendRequest, slice, envelope));

        return (cursor + 1, null);
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

        var dispatcher = _dispatchers[token.Source.AsSpan(token.StartIndex, token.Length).AsCommand()];

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