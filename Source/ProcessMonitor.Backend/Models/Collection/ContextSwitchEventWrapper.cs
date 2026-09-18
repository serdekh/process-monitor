using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace ProcessMonitor.Backend.Models.Collection;

public class ContextSwitchEventWrapper(CSwitchTraceData data) : IContextSwitchEvent
{
    private readonly CSwitchTraceData _data = data;

    public int OldThreadID => _data.OldThreadID;

    public int NewThreadID => _data.NewThreadID;
}