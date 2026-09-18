using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Channels;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Backend.State;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventCollectorContext(
    Channel<RawEvent> input,
    MonitoringSessionState state)
{
    private readonly ChannelWriter<RawEvent> _writer = input.Writer;
    private readonly MonitoringSessionState _state = state;

    public HashSet<int> TargetThreadIds { get; set; } = [];

    public int? TargetProcessId { get; set; } = null;

    public bool HasTargetProcess => TargetProcessId > 0;

    public bool TryCompleteWriting() => _writer.TryComplete();

    public Result<None, CollectionError, CollectionWarning> TryWriteRawEvent(TraceEvent data)
    {
        var kind = data.ToRawEventKind();
        
        try
        {
            var rawEvent = new RawEvent(data.Clone(), kind);

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

    public bool IsTargetProcessEvent(TraceEvent data)
    {
        return HasTargetProcess && data.ProcessID == TargetProcessId;
    }

    public bool IsTargetContextSwitch(TraceEvent data)
    {
        if (data is not CSwitchTraceData cSwitch) return false;

        return HasTargetProcess &&
            (TargetThreadIds.Contains(cSwitch.OldThreadID) ||
            TargetThreadIds.Contains(cSwitch.NewThreadID));
    }

    public Result<None, CollectionError, CollectionWarning> TryUpdateTargetProcess()
    {
        var processId = _state.ProcessId;

        if (processId == TargetProcessId)
        {
            return new Success<None, CollectionError, CollectionWarning>(new None());
        }

        TargetProcessId = processId;
        TargetThreadIds.Clear();

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

    private Result<int, CollectionError, CollectionWarning> TrySeedExistingThreads(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            foreach (ProcessThread thread in process.Threads)
            {
                TargetThreadIds.Add(thread.Id);
            }

            return new Success<int, CollectionError, CollectionWarning>(TargetThreadIds.Count);
        }
        catch (ArgumentException)
        {
            return new Success<int, CollectionError, CollectionWarning>(0)
            {
                Warnings = [new ProcessDoesNotExist(processId)]  
            };
        }
        catch (InvalidOperationException ex)
        {
            return new Failure<int, CollectionError, CollectionWarning>(
                new ErrorChain<CollectionError>(
                    new ThreadEnumerationFailed(processId, ex)));
        }
    }
}