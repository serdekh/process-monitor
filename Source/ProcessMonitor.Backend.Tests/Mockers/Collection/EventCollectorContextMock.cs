using Microsoft.Diagnostics.Tracing;
using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Models.Collection;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Tests.Mockers.Collection;

public sealed class EventCollectorContextMock : IEventCollectorContext
{
    public int? ProcessId { get; set; }

    public HashSet<int> ProcessThreadIds { get; set; } = [];

    public bool HasProcessId => ProcessId > 0;

    public bool IsContextSwitchRelevantToProcessId(IContextSwitchEvent e)
    {
        return HasProcessId &&
            (ProcessThreadIds.Contains(e.OldThreadID) ||
            ProcessThreadIds.Contains(e.NewThreadID));
    }

    public bool IsEventRelevantToProcessId(ITraceEvent e)
    {
        return HasProcessId && e.ProcessId == ProcessId;
    }

    public bool TryCompleteWriting()
    {
        throw new NotImplementedException();
    }

    public Result<int, CollectionError, CollectionWarning> TrySeedExistingThreads(int processId)
    {
        throw new NotImplementedException();
    }

    public Result<None, CollectionError, CollectionWarning> TryUpdateTargetProcess()
    {
        throw new NotImplementedException();
    }

    public Result<None, CollectionError, CollectionWarning> TryWriteRawEvent(ITraceEvent e)
    {
        throw new NotImplementedException();
    }
}