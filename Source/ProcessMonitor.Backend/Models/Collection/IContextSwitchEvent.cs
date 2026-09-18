namespace ProcessMonitor.Backend.Models.Collection;

public interface IContextSwitchEvent
{
    int OldThreadID { get; }
    int NewThreadID { get; }
}