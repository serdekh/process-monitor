namespace ProcessMonitor.Backend.State;

public sealed class MonitoringSessionState
{
    private readonly object _lock = new();

    private int? _processId = null;

    public int? ProcessId
    {
        get
        {
            lock (_lock) return _processId;
        }
    }

    public void SetProcessId(int processId)
    {
        lock (_lock) _processId = processId;
    }

    public void ResetProcessId()
    {
        lock (_lock) _processId = null;
    }
}
