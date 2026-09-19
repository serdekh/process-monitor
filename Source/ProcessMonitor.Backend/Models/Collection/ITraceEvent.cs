using Microsoft.Diagnostics.Tracing;

namespace ProcessMonitor.Backend.Models.Collection;

public interface ITraceEvent
{
    public int ProcessId { get; }

    RawEventKind GetRawEventKind();

    RawEvent CloneAsRawEvent();

    public TraceEvent Data { get; }
}
