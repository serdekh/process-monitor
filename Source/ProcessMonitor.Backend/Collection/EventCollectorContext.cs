using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;

using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Models.Collection;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Backend.State;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventCollectorContext(
    Channel<RawEvent> input,
    MonitoringSessionState state) : IEventCollectorContext
{
    private readonly ChannelWriter<RawEvent> _writer = input.Writer;
    private readonly MonitoringSessionState _state = state;

    public HashSet<int> ProcessThreadIds { get; set; } = [];

    public int? ProcessId { get; set; } = state.ProcessId;

    public bool HasProcessId => ProcessId is not null;

    public RawEvent? TryPeek() => input.Reader.TryPeek(out RawEvent e) ? e : null;

    public bool TryCompleteWriting() => _writer.TryComplete();

    public bool IsEventRelevantToProcessId(ITraceEvent e)
    {
        return HasProcessId && e.ProcessId == ProcessId;
    }

    public bool IsContextSwitchRelevantToProcessId(IContextSwitchEvent e)
    {
        return HasProcessId &&
            (ProcessThreadIds.Contains(e.OldThreadID) ||
            ProcessThreadIds.Contains(e.NewThreadID));
    }

    public Result<None, CollectionError, CollectionWarning> TryWriteRawEvent(ITraceEvent e)
    {
        var kind = e.GetRawEventKind();

        try
        {
            var rawEvent = e.CloneAsRawEvent();

            return _writer.TryWrite(rawEvent)
                ? Result.Success<None, CollectionError, CollectionWarning>(None.New())
                : Result.Success<None, CollectionError, CollectionWarning>(None.New(), [new EventWriteRejected(kind)]);
        }
        catch (Exception ex)
        {
            return Result.Failure<None, CollectionError, CollectionWarning>(new EventWriteFailed(ex, kind));
        }
    }

    public Result<None, CollectionError, CollectionWarning> TryUpdateTargetProcess()
    {
        var processId = _state.ProcessId;

        if (processId == ProcessId)
        {
            return Result.Success<None, CollectionError, CollectionWarning>(None.New());
        }

        ProcessId = processId;
        ProcessThreadIds.Clear();

        if (processId is null)
        {
            return Result.Success<None, CollectionError, CollectionWarning>(None.New(), [new NoTargetProcessConfigured()]);
        }

        var seedingResult = TrySeedExistingThreads(processId.Value);

        return seedingResult.IsFailure()
            ? Result.Failure<None, CollectionError, CollectionWarning>(new ProcessIdUpdateFailed(), seedingResult.AsFailure().Chain)
            : Result.Success<None, CollectionError, CollectionWarning>(None.New());
    }

    public Result<Process, CollectionError, CollectionWarning> TryGetProcessById(int processId)
    {
        if (processId < 0)
        {
            return Result.Failure<Process, CollectionError, CollectionWarning>(new InvalidProcessId(processId));
        }

        try
        {
            var process = Process.GetProcessById(processId);

            return Result.Success<Process, CollectionError, CollectionWarning>(process);
        }
        catch (Exception)
        {
            return Result.Failure<Process, CollectionError, CollectionWarning>(new InvalidProcessId(processId), [new ProcessDoesNotExist(processId)]);
        }
    }

    public Success<None, CollectionError, CollectionWarning> AddThreadIdsAndDispose(Process process)
    {
        foreach (ProcessThread thread in process.Threads)
        {
            ProcessThreadIds.Add(thread.Id);
        }

        process.Dispose();

        return Result.Success<None, CollectionError, CollectionWarning>(None.New());
    }

    public Result<int, CollectionError, CollectionWarning> TrySeedExistingThreads(int processId)
    {
        var result = TryGetProcessById(processId)
            .Bind(AddThreadIdsAndDispose);

        return result.IsFailure()
            ? Result.Failure<int, CollectionError, CollectionWarning>(new ThreadEnumerationFailed(processId, new ArgumentException($"Invalid process id: {processId}")), result.AsFailure().Chain)
            : Result.Success<int, CollectionError, CollectionWarning>(ProcessThreadIds.Count);
    }
}