namespace ProcessMonitor.Shared.Snapshots;

public sealed class ThreadMetricsSnapshot
{
    public int ThreadId { get; set; }

    // TODO: Implement handling the CpuTime value during
    // the processing stage.
    //public double CpuTime { get; set; }
}
