namespace ProcessMonitor.CLI.State;

public sealed class RuntimeState
{
    public int TargetPid { get; set; } = 0;

    public string BackendProcessFilePath { get; set; } = string.Empty;
}