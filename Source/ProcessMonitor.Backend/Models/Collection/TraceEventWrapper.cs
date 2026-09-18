using Microsoft.Diagnostics.Tracing;

namespace ProcessMonitor.Backend.Models.Collection;

public class TraceEventWrapper(TraceEvent data) : ITraceEvent
{
    public int ProcessId => data.ProcessID;

    public RawEvent CloneAsRawEvent() => new(data, data.ToRawEventKind());

    public RawEventKind GetRawEventKind() => data.ToRawEventKind();
}