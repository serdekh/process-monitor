using Microsoft.Extensions.Options;

using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Transport.Framing;

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ProcessMonitor.WPF.State;

public sealed class GlobalState : INotifyPropertyChanged
{
    public static readonly GlobalState Instance = new();

    public ModeState PreviousMode { get; private set; }

    private ModeState _currentMode = ModeState.Startup;

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
        _runtime ??= new ClientApplicationState(
            new FrameWriter(),
            new FrameReader(),
            new JsonMessageSerializer(),
            Options.Create(new ClientApplicationConfiguration())
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}