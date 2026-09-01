using System;
using System.Collections.Generic;

namespace ProcessMonitor.Shared.Snapshots;

public sealed class ProcessMetricsSnapshot
{
    public int ProcessId { get; set; }

    public string ProcessName { get; set; } = string.Empty;

    public double CpuUsage { get; set; }

    public int ThreadCount { get; set; }

    public int SyscallsCount { get; set; }

    public int ContextSwitchesCount { get; set; }

    public DateTime TimestampUtc { get; set; }

    public List<ThreadMetricsSnapshot> Threads { get; set; } = [];

    public override string ToString()
    {
        return $"""
            ProcessId:            |{ProcessId}
            ProcessName:          |{ProcessName}
                                  |
            CpuUsage:             |{CpuUsage}
                                  |
            ThreadCount:          |{ThreadCount}
            SyscallsCount:        |{SyscallsCount}
            ContextSwitchesCount: |{ContextSwitchesCount}
        """;
    }
}
