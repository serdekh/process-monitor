using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using ProcessMonitor.Shared.Input.Transpiling;

namespace ProcessMonitor.CLI.Input.Interpretation;

public sealed class CommandInterpreter(CommandInterpreterState state)
{
    public CommandInterpreterState State { get; set; } = state;

    public CommandInterpretersCollection InterpretersCollection = new();

    public async Task<Exception?> Interpret(List<CommandOperation> ops)
    {
        foreach (var op in ops)
        {
            var interpretationException = await InterpretersCollection.RunAsync(State, op);

            if (interpretationException is not null) return interpretationException;
        }

        return null;
    }
}