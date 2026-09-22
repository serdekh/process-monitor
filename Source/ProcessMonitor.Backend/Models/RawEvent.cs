using Microsoft.Diagnostics.Tracing;

namespace ProcessMonitor.Backend.Models;

public struct RawEvent(TraceEvent? e, RawEventKind kind)
{
    public bool HasSource { get; init; } = e is null;

    public TraceEvent? Source { get; set; } = e;

    public double TimeStampRelativeMSec { get; set; } = e?.TimeStampRelativeMSec ?? 0;

    public RawEventKind Kind { get; set; } = kind;
}