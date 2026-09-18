using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
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

    public bool TryCompleteWriting();

    public bool IsEventRelevantToProcessId(TraceEvent e);

    public bool IsContextSwitchRelevantToProcessId(IContextSwitchEvent e);

    public Result<None, CollectionError, CollectionWarning> TryWriteRawEvent(TraceEvent e);

    public Result<None, CollectionError, CollectionWarning> TryUpdateTargetProcess();

    public Result<int, CollectionError, CollectionWarning> TrySeedExistingThreads(int processId);
}