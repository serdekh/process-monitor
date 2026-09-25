using System.Collections.Generic;
using System.Diagnostics;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Models.Collection;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public interface IEventCollectorContext
{
    public int? ProcessId { get; set; }

    public HashSet<int> ProcessThreadIds { get; set; } 

    public bool HasProcessId { get; }

    public RawEvent? TryPeek();

    public bool TryCompleteWriting();

    public bool IsEventRelevantToProcessId(ITraceEvent e);

    public bool IsContextSwitchRelevantToProcessId(IContextSwitchEvent e);

    public Result<None, CollectionError, CollectionWarning> TryWriteRawEvent(ITraceEvent e);

    public Result<None, CollectionError, CollectionWarning> TryUpdateTargetProcess();

    public Result<Process, CollectionError, CollectionWarning> TryGetProcessById(int processId);

    public Success<None, CollectionError, CollectionWarning> AddThreadIdsAndDispose(Process process);

    public Result<int, CollectionError, CollectionWarning> TrySeedExistingThreads(int processId);
}