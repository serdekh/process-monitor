using Microsoft.Diagnostics.Tracing;

namespace ProcessMonitor.Backend.Models.Collection;

public class TraceEventWrapper(TraceEvent data) : ITraceEvent
{
    private readonly TraceEvent _data = data;

    public int ProcessId => _data.ProcessID;
}