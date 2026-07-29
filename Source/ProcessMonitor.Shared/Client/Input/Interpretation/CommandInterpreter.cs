using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Client.Input.Transpiling;

namespace ProcessMonitor.Shared.Client.Input.Interpretation;

public sealed class CommandInterpreter(ClientApplicationState state)
{
    public ClientApplicationState State { get; set; } = state;

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