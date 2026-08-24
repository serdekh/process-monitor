using Microsoft.Diagnostics.Tracing;

namespace ProcessMonitor.Backend.Models;

public struct RawEvent(TraceEvent e, RawEventKind kind)
{
    public TraceEvent Source { get; set; } = e.Clone();

    public RawEventKind Kind { get; set; } = kind;
}