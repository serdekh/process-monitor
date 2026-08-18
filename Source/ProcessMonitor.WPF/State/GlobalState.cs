using Microsoft.Extensions.Options;

using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Transport.Framing;

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;

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
            _runtime ??= new ClientApplicationState(
                new FrameWriter(),
                new FrameReader(),
                new JsonMessageSerializer(),
                Options.Create(new ClientApplicationConfiguration())
            );

            return _runtime;
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}