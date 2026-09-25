using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Models.Collection;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventHandlerDispatcher : IEventHandlerDispatcher
{
    private readonly Dictionary<RawEventKind, EventHandlerFunc> _handlers;

    private readonly IEventCollectorContext _ctx;

    public EventHandlerDispatcher(IEventCollectorContext ctx)
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

    public Result<None, CollectionError, CollectionWarning> DispatchEvent(ITraceEvent e)
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

        var kind = e.GetRawEventKind();

        if (_handlers.TryGetValue(kind, out EventHandlerFunc? value))
        {
            var result = value(e);

            if (result is Failure<None, CollectionError, CollectionWarning> handlingFailure)
            {
                return new Failure<None, CollectionError, CollectionWarning>(
                    new ErrorChain<CollectionError>(
                        new EventDispatchingFailed(), handlingFailure.Chain));
            }
        }

        return new Success<None, CollectionError, CollectionWarning>(new None());
    }

    public Result<None, CollectionError, CollectionWarning> HandleEvent(Func<bool> isRelevant, Action handler, ITraceEvent e)
    {
        if (!isRelevant())
        {
            return new Success<None, CollectionError, CollectionWarning>(new None());
        }

        handler();

        var kind = e.GetRawEventKind();

        var writeEventResult = _ctx.TryWriteRawEvent(e);

        if (writeEventResult is Failure<None, CollectionError, CollectionWarning> failure)
        {
            return new Failure<None, CollectionError, CollectionWarning>(
                new ErrorChain<CollectionError>(
                    new EventHandlingError(kind), failure.Chain));
        }

        return new Success<None, CollectionError, CollectionWarning>(new None());
    }

    public Result<None, CollectionError, CollectionWarning> HandleThreadStart(ITraceEvent e)
    {
        return HandleEvent
        (
            () => _ctx.IsEventRelevantToProcessId(e),
            () => _ctx.ProcessThreadIds.Add(e.Data.ThreadID),
            e
        );
    }

    public Result<None, CollectionError, CollectionWarning> HandleThreadDCStart(ITraceEvent e)
    {
        return HandleEvent
        (
            () => _ctx.IsEventRelevantToProcessId(e),
            () => _ctx.ProcessThreadIds.Add(e.Data.ThreadID),
            e
        );
    }

    public Result<None, CollectionError, CollectionWarning> HandleThreadStop(ITraceEvent e)
    {
        return HandleEvent
        (
            () => _ctx.ProcessThreadIds.Contains(e.Data.ThreadID),
            () => _ctx.ProcessThreadIds.Remove(e.Data.ThreadID),
            e
        );
    }

    public Result<None, CollectionError, CollectionWarning> HandleThreadDCEnd(ITraceEvent e)
    {
        return HandleEvent
        (
            () => _ctx.ProcessThreadIds.Contains(e.Data.ThreadID),
            () => _ctx.ProcessThreadIds.Remove(e.Data.ThreadID),
            e
        );
    }

    public Result<None, CollectionError, CollectionWarning> HandleContextSwitch(ITraceEvent e)
    {
        return HandleEvent
        (
            () => _ctx.IsContextSwitchRelevantToProcessId(
                new ContextSwitchEventWrapper((CSwitchTraceData)e.Data)),
            () => {},
            e
        );
    }

    public Result<None, CollectionError, CollectionWarning> HandleSyscallEnter(ITraceEvent e)
    {
        return HandleEvent
        (
            () => _ctx.IsEventRelevantToProcessId(e),
            () => {},
            e
        );
    }    
    
    public Result<None, CollectionError, CollectionWarning> HandleUndefined(ITraceEvent e)
    {
        return new Success<None, CollectionError, CollectionWarning>(new None());
    }
}