namespace ProcessMonitor.Backend.Models.Collection;

public interface ITraceEvent
{
    public int ProcessId { get; }

    RawEventKind GetRawEventKind();

    RawEvent CloneAsRawEvent();
}
