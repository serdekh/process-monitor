using Microsoft.Extensions.Options;

using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Transport.Framing;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ProcessMonitor.WPF.State;

public sealed class GlobalState : INotifyPropertyChanged
{
    public static readonly GlobalState Instance = new();

    public ModeState PreviousMode { get; private set; }

    private ModeState _currentMode = ModeState.Startup;

    private ObservableCollection<ThreadMetricsSnapshot> _threadMetrics = [];

    public ModeState CurrentMode 
    {
        get { return _currentMode; }
        set
        {
            if (value == _currentMode) return;

            PreviousMode = _currentMode;
            _currentMode = value;
            OnPropertyChanged();
        }
    }

    public ClientApplicationConfiguration Configuration { get; } = new();

    private ClientApplicationState? _runtime;

    public ClientApplicationState Runtime
    {
        get
        {
            // Note: This if-check is required to prevent WPF engine to 
            // recursively initialize GlobalState class. Do not remove
            string processName = Process.GetCurrentProcess().ProcessName.ToLower();

            if (processName.Contains("wpfsurface") || processName.Contains("xdesproc") || processName.Contains("devenv"))
            {
                return null!;
            }

            return CreateRuntime();
        }
    }

    public ProcessMetricsSnapshot? LatestSnapshot 
    {
        get { return _runtime?.LatestSnapshot; } 
        set 
        { 
            if (_runtime != null)
            {
                _runtime.LatestSnapshot = value;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    _threadMetrics.Clear();
                    if (value is not null)
                    {
                        foreach (var thread in value.Threads)
                        {
                            _threadMetrics.Add(thread);
                        }
                    }
                });
                OnPropertyChanged();
                OnPropertyChanged(nameof(LatestSnapshotThreads));
                OnPropertyChanged(nameof(LatestSnapshotCpuUsage));
                OnPropertyChanged(nameof(LatestSnapshotSyscallsCount));
                OnPropertyChanged(nameof(LatestSnapshotContextSwitchesCount));
            }
        } 
    }

    public ObservableCollection<ThreadMetricsSnapshot> LatestSnapshotThreads =>_threadMetrics;

    public double LatestSnapshotCpuUsage
    {
        get
        {
            return LatestSnapshot is null ? 0 : LatestSnapshot.CpuUsage;
        }
        set
        {
            if (LatestSnapshot is not null)
            {
                LatestSnapshot.CpuUsage = value;
                OnPropertyChanged();
            }
        }
    }

    public int LatestSnapshotSyscallsCount
    {
        get
        {
            return LatestSnapshot is null ? 0 : LatestSnapshot.SyscallsCount;
        }
        set
        {
            if (LatestSnapshot is not null)
            {
                LatestSnapshot.SyscallsCount = value;
                OnPropertyChanged();
            }
        }
    }

    public int LatestSnapshotContextSwitchesCount 
    {
        get
        {
            return LatestSnapshot is null ? 0 : LatestSnapshot.ContextSwitchesCount;
        }
        set
        {
            if (LatestSnapshot is not null)
            {
                LatestSnapshot.ContextSwitchesCount = value;
                OnPropertyChanged();
            }
        }
    }

    private uint _latestRequestId = 0;

    public uint LatestRequestId => _latestRequestId;

    public OverflowException? IncrementLatestRequestId()
    {
        try
        {
            checked { _latestRequestId++; }
            return null;
        }
        catch (OverflowException ex)
        {
            return ex;
        }
    }

    private ClientApplicationState CreateRuntime()
    {
        // TODO: This options creation statement is run on every
        // time a new instance is required. In case of this project
        // it happens every time a user presses the 'Run' button. 
        // For now it's not a big deal but it would've been improved
        // if configuration properties were cacheable
        var configuration = Options.Create(Configuration);

        _runtime ??= new ClientApplicationState(
            new FrameWriter(),
            new FrameReader(),
            new JsonMessageSerializer(),
            configuration
        );

        return _runtime;
    }

    public async Task<Exception?> TryInitializeRuntime()
    {
        var runtime = CreateRuntime();

        var backendCreationException = runtime.Backend.TryCreate();

        if (backendCreationException is not null) return backendCreationException;

        var commandsPipeException = runtime.CommandsPipe.TryInitialize();

        if (commandsPipeException is not null)
        {
            await runtime.Cleanup();
            return commandsPipeException;
        }

        var telemetryPipeException = runtime.TelemetryPipe.TryInitialize();

        if (telemetryPipeException is not null)
        {
            await runtime.Cleanup();
            return telemetryPipeException;
        }

        var commandsPipeConnectionException = await runtime.CommandsPipe.TryConnectAsync(runtime.CancellationToken);

        if (commandsPipeConnectionException is not null)
        {
            await runtime.Cleanup();
            return commandsPipeConnectionException;
        }

        var telemetryPipeConnectionException = await runtime.TelemetryPipe.TryConnectAsync(runtime.CancellationToken);

        if (telemetryPipeConnectionException is not null)
        {
            await runtime.Cleanup();
            return telemetryPipeConnectionException;
        }

        return null;
    }

    public void FinalizeRuntime() => _runtime?.Cleanup();

    public Exception? IsRuntimeInitialized()
    {
        if (_runtime == null) return new InvalidOperationException("No runtime was initialized");

        if (!_runtime.Backend.IsRunning) return new InvalidOperationException("The backend process was not initialized");

        if (!_runtime.CommandsPipe.IsConnected()) return new InvalidOperationException("The commands pipe was not initialized");

        if (!_runtime.TelemetryPipe.IsConnected()) return new InvalidOperationException("The telemetry pipe was not initialized");

        return null;
    }

    public async Task<Exception?> StartProcessing()
    {
        var runtimeInitializationException = IsRuntimeInitialized();

        if (runtimeInitializationException is not null) return runtimeInitializationException;

        JsonElement body;

        try
        {
            body = JsonSerializer.SerializeToElement(new { version = 0.1, requestId = LatestRequestId, pid = Runtime.Configuration.ProcessId });
        }
        catch (Exception ex) 
        {
            return ex;
        }

        var request = new CommandRequest()
        {
            Method = "post",
            Route = "monitoring",
            Body = body
        };

        var envelope = new MessageEnvelope<CommandRequest>()
        {
            Type = MessageType.CommandRequest,
            Payload = request
        };

        var writingException = await _runtime!.CommandsPipe.TryWriteAsync(envelope, _runtime.CancellationToken);

        if (writingException is not null) return writingException;

        CurrentMode = ModeState.Running;

        _ = Task.Run(async () =>
        {
            while (!_runtime.CancellationToken.IsCancellationRequested)
            {
                (var latest, var telemetryReadingException) = await _runtime.TelemetryPipe.TryReadAsync<ProcessMetricsSnapshot>(_runtime.CancellationToken);

                if (telemetryReadingException is not null)
                {
                    CurrentMode = ModeState.Startup;
                    return;
                }

                if (latest != null)
                {
                    LatestSnapshot = latest.Payload;
                }

                try
                {
                    await Task.Delay(50, _runtime.CancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, _runtime.CancellationToken);

        return null;
    }

    public async Task<Exception?> StopProcessing()
    {
        var runtimeInitializationException = IsRuntimeInitialized();

        if (runtimeInitializationException is not null) return runtimeInitializationException;

        JsonElement body;

        try
        {
            body = JsonSerializer.SerializeToElement(new { version = 0.1, requestId = LatestRequestId, pid = 0 });
        }
        catch (Exception ex)
        {
            return ex;
        }

        var request = new CommandRequest()
        {
            Method = "delete",
            Route = "monitoring",
            Body = body
        };

        var envelope = new MessageEnvelope<CommandRequest>()
        {
            Type = MessageType.CommandRequest,
            Payload = request
        };

        var writingException = await _runtime!.TelemetryPipe.TryWriteAsync(envelope, _runtime.CancellationToken);

        if (writingException is not null) return writingException;

        CurrentMode = ModeState.Startup;

        return null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}