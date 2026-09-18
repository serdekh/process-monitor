using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
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

    public int? ProcessId { get; set; } = null;

    public bool HasProcessId => ProcessId > 0;

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

            if (_writer.TryWrite(rawEvent)) 
            {
                return new Success<None, CollectionError, CollectionWarning>(new None());
            }

            return new Success<None, CollectionError, CollectionWarning>(new None())
            {
                Warnings = [ new EventWriteRejected(kind)]
            };
        }
        catch (Exception ex)
        {
            return new Failure<None, CollectionError, CollectionWarning>(
                new ErrorChain<CollectionError>(
                    new EventWriteFailed(ex, kind)));
        }
    }  

    public Result<None, CollectionError, CollectionWarning> TryUpdateTargetProcess()
    {
        var processId = _state.ProcessId;

        if (processId == ProcessId)
        {
            return new Success<None, CollectionError, CollectionWarning>(new None());
        }

        ProcessId = processId;
        ProcessThreadIds.Clear();

        if (processId is null)
        {
            return new Success<None, CollectionError, CollectionWarning>(new None())
            {
                Warnings = [new NoTargetProcessConfigured()]
            };
        }

        if (TrySeedExistingThreads(processId.Value) is Failure<int, CollectionError, CollectionWarning> failure)
        {
            return new Failure<None, CollectionError, CollectionWarning>(
                new ErrorChain<CollectionError>(
                    new ProcessIdUpdateFailed(), failure.Chain));
        }

        return new Success<None, CollectionError, CollectionWarning>(new None());
    }

    public Result<int, CollectionError, CollectionWarning> TrySeedExistingThreads(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            foreach (ProcessThread thread in process.Threads)
            {
                ProcessThreadIds.Add(thread.Id);
            }

            return new Success<int, CollectionError, CollectionWarning>(ProcessThreadIds.Count);
        }
        catch (ArgumentException)
        {
            return new Success<int, CollectionError, CollectionWarning>(0)
            {
                Warnings = [new ProcessDoesNotExist(processId)]  
            };
        }
        catch (Exception ex)
        {
            return new Failure<int, CollectionError, CollectionWarning>(
                new ErrorChain<CollectionError>(
                    new ThreadEnumerationFailed(processId, ex)));
        }
    }
}