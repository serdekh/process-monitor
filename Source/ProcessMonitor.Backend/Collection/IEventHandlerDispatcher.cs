using System;
using Microsoft.Diagnostics.Tracing;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public interface IEventHandlerDispatcher
{
    public Result<None, CollectionError, CollectionWarning> DispatchEvent(TraceEvent data);

    protected Result<None, CollectionError, CollectionWarning> HandleEvent(
        Func<bool> isRelevant, Action handler, TraceEvent data);

    protected Result<None, CollectionError, CollectionWarning> HandleThreadStart(TraceEvent data);

    protected Result<None, CollectionError, CollectionWarning> HandleThreadDCStart(TraceEvent data);

    protected Result<None, CollectionError, CollectionWarning> HandleThreadStop(TraceEvent data);

    protected Result<None, CollectionError, CollectionWarning> HandleThreadDCEnd(TraceEvent data);

    protected Result<None, CollectionError, CollectionWarning> HandleContextSwitch(TraceEvent data);

    protected Result<None, CollectionError, CollectionWarning> HandleSyscallEnter(TraceEvent data);
    
    protected Result<None, CollectionError, CollectionWarning> HandleUndefined(TraceEvent data);
}