using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using ProcessMonitor.Backend.Collection;
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

    public bool IsContextSwitchRelevantToProcessId(CSwitchTraceData e)
    {
        throw new NotImplementedException();
    }

    public bool IsEventRelevantToProcessId(TraceEvent e)
    {
        return HasProcessId && e.ProcessID == ProcessId;
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

    public Result<None, CollectionError, CollectionWarning> TryWriteRawEvent(TraceEvent e)
    {
        throw new NotImplementedException();
    }
}