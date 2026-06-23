using System;

namespace ProcessMonitor.Backend.Models;

public sealed class RawKernelEvent
{
    public int ProcessId { get; init; }

    public DateTime Timestamp { get; init; } 

    public string EventName { get; init; } = string.Empty;
}
