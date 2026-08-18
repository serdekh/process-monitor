using Microsoft.Extensions.Options;

using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Transport.Framing;

using System.ComponentModel;
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

    private readonly ClientApplicationState _runtime = new
    (
        new FrameWriter(),
        new FrameReader(),
        new JsonMessageSerializer(),
        Options.Create(new ClientApplicationConfiguration())
    );

    public ClientApplicationState Runtime => _runtime;

    public ProcessMetricsSnapshot? LatestSnapshot 
    {
        get { return _runtime.LatestSnapshot; } 
        set { _runtime.LatestSnapshot = value; OnPropertyChanged(); } 
    }

    private uint _latestRequestId = 0;

    public uint LatestRequestId
    {
        get 
        { 
            var temp = _latestRequestId; _latestRequestId++;
            OnPropertyChanged();
            return temp;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}