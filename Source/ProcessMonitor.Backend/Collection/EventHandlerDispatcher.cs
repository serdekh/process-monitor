using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventHandlerDispatcher
{
    private Dictionary<RawEventKind, EventHandlerFunc> _handlers;

    private EventCollectorContext _ctx;

    public EventHandlerDispatcher(EventCollectorContext ctx)
    {
        _ctx = ctx;

        _handlers = new Dictionary<RawEventKind, EventHandlerFunc>()
        {
            [RawEventKind.ContextSwitch] = HandleContextSwitch,
            [RawEventKind.SyscallEnter] = HandleSyscallEnter,
            [RawEventKind.ThreadDCEnd] = HandleThreadDCEnd,
            [RawEventKind.ThreadDCStart] = HandleThreadDCStart,
            [RawEventKind.ThreadStart] = HandleThreadStart,
            [RawEventKind.ThreadStop] = HandleThreadStop,
            [RawEventKind.Undefined] = HandleUndefined
        };
    }

    public Result<None, CollectionError, CollectionWarning> DispatchEvent(TraceEvent data)
    {
        if (_ctx.TryUpdateTargetProcess() is Failure<None, CollectionError, CollectionWarning> failure)
        {
            return new Failure<None, CollectionError, CollectionWarning>(
                new ErrorChain<CollectionError>(
                    new EventDispatchingFailed(), failure.Chain));
        }
        
        if (!_ctx.HasProcessId)
        {
            return new Success<None, CollectionError, CollectionWarning>(new None());
        }

        var kind = data.ToRawEventKind();

        if (_handlers.TryGetValue(kind, out EventHandlerFunc? value))
        {
            value(data);
        }

        return new Success<None, CollectionError, CollectionWarning>(new None());
    }

    private Result<None, CollectionError, CollectionWarning> HandleEvent(Func<bool> isRelevant, Action handler, TraceEvent data)
    {
        if (!isRelevant())
        {
            return new Success<None, CollectionError, CollectionWarning>(new None());
        }

        handler();

        var kind = data.ToRawEventKind();

        var writeEventResult = _ctx.TryWriteRawEvent(data);

        if (writeEventResult is Failure<None, CollectionError, CollectionWarning> failure)
        {
            return new Failure<None, CollectionError, CollectionWarning>(
                new ErrorChain<CollectionError>(
                    new EventHandlingError(kind), failure.Chain));
        }

        return new Success<None, CollectionError, CollectionWarning>(new None());
    }

    private Result<None, CollectionError, CollectionWarning> HandleThreadStart(TraceEvent data)
    {
        return HandleEvent
        (
            () => _ctx.IsEventRelevantToProcessId(data),
            () => _ctx.ProcessThreadIds.Add(data.ThreadID),
            data
        );
    }

    private Result<None, CollectionError, CollectionWarning> HandleThreadDCStart(TraceEvent data)
    {
        return HandleEvent
        (
            () => _ctx.IsEventRelevantToProcessId(data),
            () => _ctx.ProcessThreadIds.Add(data.ThreadID),
            data
        );
    }

    private Result<None, CollectionError, CollectionWarning> HandleThreadStop(TraceEvent data)
    {
        return HandleEvent
        (
            () => _ctx.ProcessThreadIds.Contains(data.ThreadID),
            () => _ctx.ProcessThreadIds.Remove(data.ThreadID),
            data
        );
    }

    private Result<None, CollectionError, CollectionWarning> HandleThreadDCEnd(TraceEvent data)
    {
        return HandleEvent
        (
            () => _ctx.ProcessThreadIds.Contains(data.ThreadID),
            () => _ctx.ProcessThreadIds.Remove(data.ThreadID),
            data
        );
    }

    private Result<None, CollectionError, CollectionWarning> HandleContextSwitch(TraceEvent data)
    {
        return HandleEvent
        (
            () => _ctx.IsContextSwitchRelevantToProcessId((CSwitchTraceData)data),
            () => {},
            data
        );
    }

    private Result<None, CollectionError, CollectionWarning> HandleSyscallEnter(TraceEvent data)
    {
        return HandleEvent
        (
            () => _ctx.IsEventRelevantToProcessId(data),
            () => {},
            data
        );
    }    
    
    private Result<None, CollectionError, CollectionWarning> HandleUndefined(TraceEvent data)
    {
        return new Success<None, CollectionError, CollectionWarning>(new None());
    }
}