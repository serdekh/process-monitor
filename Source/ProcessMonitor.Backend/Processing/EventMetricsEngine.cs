using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.State;
using ProcessMonitor.Shared.Snapshots;

namespace ProcessMonitor.Backend.Processing;

public sealed class EventMetricsEngine(
    Channel<RawEvent> input,
    Channel<ProcessMetricsSnapshot> output,
    ILogger<EventMetricsEngine> logger,
    MonitoringSessionState state)
{
    private const double BucketDurationMs = 400.0;

    private readonly ChannelReader<RawEvent> _input = input.Reader;
    private readonly ChannelWriter<ProcessMetricsSnapshot> _output = output.Writer;
    private readonly ILogger<EventMetricsEngine> _logger = logger;
    private readonly MonitoringSessionState _state = state;

    private readonly Dictionary<int, ThreadMetricsAccumulator> _threads = [];

    private int _targetProcessId = -1;

    private int _syscallsCount;
    private int _contextSwitchesCount;

    private double _bucketStartMs;
    private double _bucketEndMs;

    private bool _bucketInitialized;

    private string _processName = "(null)";

    private int CurrentProcessId => _state.ProcessId ?? -1;

    private bool HasTargetProcess => _targetProcessId > 0;

    private void ResetState()
    {
        _threads.Clear();

        _syscallsCount = 0;
        _contextSwitchesCount = 0;

        _bucketInitialized = false;
        _bucketStartMs = 0;
        _bucketEndMs = 0;

        _processName = "(null)";
    }

    private bool TryUpdateTargetProcess()
    {
        var processId = CurrentProcessId;

        if (processId == _targetProcessId) return false;

        ResetState();

        _targetProcessId = processId;

        if (_targetProcessId <= 0)
        {
            _logger.LogInformation("[Processing]: No target process configured.");

            return true;
        }

        TryInitializeProcessMetadata(_targetProcessId);
        SeedExistingThreads(_targetProcessId);

        _logger.LogInformation("[Processing]: Target process changed to PID {ProcessId}.", _targetProcessId);

        return true;
    }

    private void TryInitializeProcessMetadata(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            _processName = process.ProcessName;
        }
        catch (ArgumentException)
        {
            _processName = "(null)";
        }
        catch (InvalidOperationException)
        {
            _processName = "(null)";
        }
    }

    private void SeedExistingThreads(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            foreach (ProcessThread processThread in process.Threads)
            {
                var threadId = processThread.Id;

                _threads.TryAdd(
                    threadId,
                    new ThreadMetricsAccumulator
                    {
                        ThreadId = threadId,
                        IsAlive = true,
                        IsRunning = false,
                        ScheduledInTimestampMs = 0
                    });
            }

            _logger.LogDebug("[Processing]: Seeded {ThreadCount} threads for PID {ProcessId}.", 
                _threads.Count,
                processId);
        }
        catch (ArgumentException)
        {
            _logger.LogDebug("[Processing]: Process {ProcessId} does not exist.", processId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogDebug(ex, "[Processing]: Could not enumerate threads for PID {ProcessId}.", processId);
        }
    }

    private void InitializeBucket(double timestampMs)
    {
        if (_bucketInitialized)
            return;

        _bucketStartMs = timestampMs;
        _bucketEndMs = timestampMs + BucketDurationMs;
        _bucketInitialized = true;

        foreach (var thread in _threads.Values)
        {
            thread.ScheduledInTimestampMs = timestampMs;
        }
    }

    private void AccumulateCpu(ThreadMetricsAccumulator thread, double endTimestampMs)
    {
        if (!thread.IsRunning) return;

        if (!_bucketInitialized) return;

        var start = Math.Max(thread.ScheduledInTimestampMs, _bucketStartMs);

        var end = Math.Min(endTimestampMs, _bucketEndMs);

        if (end > start)
        {
            thread.BucketCpuTimeMs += end - start;
        }
    }

    private void HandleThreadStart(ThreadTraceData data)
    {
        if (data.ProcessID != _targetProcessId) return;

        var timestamp = data.TimeStampRelativeMSec;

        if (_threads.TryGetValue(data.ThreadID, out var existing))
        {
            existing.IsAlive = true;
            existing.IsRunning = false;
            existing.ScheduledInTimestampMs = timestamp;
            return;
        }

        _threads.Add(
            data.ThreadID,
            new ThreadMetricsAccumulator
            {
                ThreadId = data.ThreadID,
                IsAlive = true,
                IsRunning = false,
                ScheduledInTimestampMs = timestamp
            });
    }

    private void HandleThreadStop(ThreadTraceData data)
    {
        if (!_threads.TryGetValue(data.ThreadID, out var thread)) return;

        var timestamp = data.TimeStampRelativeMSec;

        if (thread.IsRunning)
        {
            AccumulateCpu(thread, timestamp);
            thread.IsRunning = false;
        }

        thread.IsAlive = false;
    }

    private void HandleContextSwitch(CSwitchTraceData data)
    {
        var timestamp = data.TimeStampRelativeMSec;

        if (_threads.TryGetValue(data.OldThreadID, out var oldThread))
        {
            if (oldThread.IsAlive && oldThread.IsRunning)
            {
                AccumulateCpu(oldThread, timestamp);
                oldThread.IsRunning = false;
            }
        }

        if (_threads.TryGetValue(data.NewThreadID, out var newThread))
        {
            if (newThread.IsAlive)
            {
                newThread.IsRunning = true;
                newThread.ScheduledInTimestampMs = timestamp;
            }
        }

        _contextSwitchesCount++;
    }

    private void HandleSyscallEnter()
    {
        _syscallsCount++;
    }

    private void HandleRawEvent(RawEvent rawEvent)
    {
        if (rawEvent.Source is null)
            return;

        switch (rawEvent.Kind)
        {
            case RawEventKind.ContextSwitch:
            {
                if (rawEvent.Source is CSwitchTraceData contextSwitch)
                    HandleContextSwitch(contextSwitch);

                break;
            }

            case RawEventKind.ThreadStart:
            case RawEventKind.ThreadDCStart:
            {
                if (rawEvent.Source is ThreadTraceData threadStart)
                    HandleThreadStart(threadStart);

                break;
            }

            case RawEventKind.ThreadStop:
            case RawEventKind.ThreadDCEnd:
            {
                if (rawEvent.Source is ThreadTraceData threadStop)
                    HandleThreadStop(threadStop);

                break;
            }

            case RawEventKind.SyscallEnter:
                HandleSyscallEnter();
                break;

            case RawEventKind.SyscallExit:
            case RawEventKind.Undefined:

            default: break;
        }
    }

    private void FinalizeRunningThreads(double bucketEndMs)
    {
        foreach (var thread in _threads.Values)
        {
            if (!thread.IsAlive || !thread.IsRunning)
                continue;

            AccumulateCpu(thread, bucketEndMs);

            thread.ScheduledInTimestampMs = bucketEndMs;
        }
    }

    private ProcessMetricsSnapshot BuildSnapshot()
    {
        var cpuTimeMs = 0.0;

        foreach (var thread in _threads.Values)
        {
            cpuTimeMs += thread.BucketCpuTimeMs;
        }

        var aliveThreads = _threads.Values
            .Where(thread => thread.IsAlive)
            .ToArray();

        return new ProcessMetricsSnapshot
        {
            ProcessId = _targetProcessId,
            ProcessName = _processName,

            SyscallsCount = _syscallsCount,
            ContextSwitchesCount = _contextSwitchesCount,

            CpuUsage = cpuTimeMs / BucketDurationMs * 100.0,

            ThreadCount = aliveThreads.Length,

            TimestampUtc = DateTime.UtcNow,

            Threads =
            [
                .. aliveThreads.Select(thread =>
                    new ThreadMetricsSnapshot
                    {
                        ThreadId = thread.ThreadId,
                        ScheduledInTimestampMs =
                            thread.ScheduledInTimestampMs,
                        IsRunning = thread.IsRunning,
                        IsAlive = thread.IsAlive,
                        CpuTimeMs = thread.BucketCpuTimeMs
                    })
            ]
        };
    }

    private void ResetBucketMetrics()
    {
        _syscallsCount = 0;
        _contextSwitchesCount = 0;

        foreach (var thread in _threads.Values)
        {
            thread.BucketCpuTimeMs = 0;
        }

        foreach (var threadId in _threads
            .Where(x => !x.Value.IsAlive)
            .Select(x => x.Key)
            .ToArray())
        {
            _threads.Remove(threadId);
        }
    }

    private async ValueTask EmitBucket(CancellationToken ct)
    {
        if (!_bucketInitialized || !HasTargetProcess) return;

        FinalizeRunningThreads(_bucketEndMs);

        var snapshot = BuildSnapshot();

        Console.WriteLine(snapshot);

        await _output.WriteAsync(
            snapshot,
            ct);

        ResetBucketMetrics();
    }

    private async Task ProcessEvents(CancellationToken ct)
    {
        while (await _input.WaitToReadAsync(ct))
        {
            while (_input.TryRead(out var rawEvent))
            {
                TryUpdateTargetProcess();

                if (!HasTargetProcess) continue;

                if (rawEvent.Source is null) continue;

                var timestamp = rawEvent.Source.TimeStampRelativeMSec;

                InitializeBucket(timestamp);

                while (timestamp >= _bucketEndMs)
                {
                    await EmitBucket(ct);

                    _bucketStartMs = _bucketEndMs;
                    _bucketEndMs =
                        _bucketStartMs + BucketDurationMs;
                }

                HandleRawEvent(rawEvent);
            }
        }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        try
        {
            TryUpdateTargetProcess();

            await ProcessEvents(ct);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("[Processing]: Cancellation requested.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Processing]: Could not process events.");
        }
    }
}