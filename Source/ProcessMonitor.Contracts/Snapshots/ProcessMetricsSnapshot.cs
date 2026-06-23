using System;
using System.Collections.Generic;

namespace ProcessMonitor.Contracts.Snapshots;

public sealed class ProcessMetricsSnapshot
{
    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;

    public double CpuUsage { get; init; }

    public int ThreadCount { get; init; }

    public DateTime TimestampUtc { get; init; }

    public List<ThreadMetricsSnapshot> Threads { get; set; } = new();
}
