namespace ProcessMonitor.Shared.Snapshots;

public sealed class ThreadMetricsSnapshot
{
    public int ThreadId { get; set; }

    public double ScheduledInTimestampMs { get; set; }

    public bool IsRunning { get; set; }

    public double CpuTimeMs { get; set; }

    public bool IsAlive { get; set; }
}
