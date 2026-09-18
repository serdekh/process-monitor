namespace ProcessMonitor.Backend.Models.Collection;

public interface IContextSwitchEvent
{
    public int OldThreadID { get; }
    public int NewThreadID { get; }
}