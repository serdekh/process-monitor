namespace ProcessMonitor.Backend.Models;

public sealed class ThreadMetricsAccumulator
{
    public int ThreadId { get; init; }

    public bool IsAlive { get; set; }

    public bool IsRunning { get; set; }

    public double ScheduledInTimestampMs { get; set; }

    public double BucketCpuTimeMs { get; set; }
}