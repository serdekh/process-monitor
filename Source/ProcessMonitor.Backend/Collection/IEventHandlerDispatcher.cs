using System;
using Microsoft.Diagnostics.Tracing;
using ProcessMonitor.Backend.Models.Collection;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public interface IEventHandlerDispatcher
{
    public Result<None, CollectionError, CollectionWarning> DispatchEvent(ITraceEvent data);

    public Result<None, CollectionError, CollectionWarning> HandleEvent(
        Func<bool> isRelevant, Action handler, ITraceEvent data);

    public Result<None, CollectionError, CollectionWarning> HandleThreadStart(ITraceEvent data);

    public Result<None, CollectionError, CollectionWarning> HandleThreadDCStart(ITraceEvent data);

    public Result<None, CollectionError, CollectionWarning> HandleThreadStop(ITraceEvent data);

    public Result<None, CollectionError, CollectionWarning> HandleThreadDCEnd(ITraceEvent data);

    public Result<None, CollectionError, CollectionWarning> HandleContextSwitch(ITraceEvent data);

    public Result<None, CollectionError, CollectionWarning> HandleSyscallEnter(ITraceEvent data);
    
    public Result<None, CollectionError, CollectionWarning> HandleUndefined(ITraceEvent data);
}