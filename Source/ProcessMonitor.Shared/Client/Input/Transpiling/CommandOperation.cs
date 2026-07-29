using ProcessMonitor.Shared.Client.Input.Lexing;

namespace ProcessMonitor.Shared.Client.Input.Transpiling;

public struct CommandOperation(CommandOperationType Type, TokenSlice SourceToken, object? Argument = null)
{
    public CommandOperationType Type { get; set; } = Type;

    public TokenSlice SourceToken { get; set; } = SourceToken;

    public object? Argument {get; set; } = Argument; 
}