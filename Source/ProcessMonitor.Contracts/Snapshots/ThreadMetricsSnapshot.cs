namespace ProcessMonitor.Contracts.Snapshots;

public sealed class ThreadMetricsSnapshot
{
    public int ThreadId { get; init; }

    public int ProcessId { get; init; }

    public double CpuTime { get; init; }
}
