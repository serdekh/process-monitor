using System;
using System.Collections.Generic;

namespace ProcessMonitor.Shared.Snapshots;

public sealed class ProcessMetricsSnapshot
{
    public int ProcessId { get; init; }

    public string ProcessName { get; init; } = string.Empty;

    public double CpuUsage { get; init; }

    public int ThreadCount { get; init; }

    public DateTime TimestampUtc { get; init; }

    public List<ThreadMetricsSnapshot> Threads { get; set; } = new();

    // TODO: Add more fields and use a string builder for a dynamic thread colletion
    public override string ToString()
    {
        return $"{{ ProcessId: {ProcessId}, ProcessName: {ProcessName}, CpuUsage: {CpuUsage}, ThreadCount: {ThreadCount}}}";
    }
}
