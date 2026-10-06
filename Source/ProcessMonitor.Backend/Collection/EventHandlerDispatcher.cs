using System;
using System.Collections.Generic;

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
        var updateResult = _ctx.TryUpdateTargetProcess();

        if (updateResult.IsFailure())
        {
            return Result.Failure<None, CollectionError, CollectionWarning>(new EventDispatchingFailed(), updateResult.AsFailure().Chain);
        }

        if (!_ctx.HasProcessId)
        {
            return Result.Success<None, CollectionError, CollectionWarning>(None.New());
        }

        var kind = e.GetRawEventKind();

        if (_handlers.TryGetValue(kind, out EventHandlerFunc? value))
        {
            var result = value(e);

            if (result.IsFailure())
            {
                return Result.Failure<None, CollectionError, CollectionWarning>(new EventDispatchingFailed(), result.AsFailure().Chain);
            }
        }

        return Result.Success<None, CollectionError, CollectionWarning>(None.New());
    }

    public Result<None, CollectionError, CollectionWarning> HandleEvent(Func<bool> isRelevant, Action handler, ITraceEvent e)
    {
        if (!isRelevant())
        {
            return Result.Success<None, CollectionError, CollectionWarning>(None.New());
        }

        handler();

        var kind = e.GetRawEventKind();

        var writeEventResult = _ctx.TryWriteRawEvent(e);

        return writeEventResult switch
        {
            Failure<None, CollectionError, CollectionWarning> failure => Result.Failure<None, CollectionError, CollectionWarning>(new EventHandlingError(kind), failure.Chain),
            Success<None, CollectionError, CollectionWarning> => Result.Success<None, CollectionError, CollectionWarning>(None.New()),
            _ => throw new InvalidOperationException()
        };
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
            () => { },
            e
        );
    }

    public Result<None, CollectionError, CollectionWarning> HandleSyscallEnter(ITraceEvent e)
    {
        return HandleEvent
        (
            () => _ctx.IsEventRelevantToProcessId(e),
            () => { },
            e
        );
    }

    public Result<None, CollectionError, CollectionWarning> HandleUndefined(ITraceEvent e)
    {
        return Result.Success<None, CollectionError, CollectionWarning>(None.New());
    }
}