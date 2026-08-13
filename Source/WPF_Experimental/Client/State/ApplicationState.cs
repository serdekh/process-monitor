using ProcessMonitor.Shared.Snapshots;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WPF_Experimental.Client.State;

public sealed class ApplicationState : INotifyPropertyChanged
{
    public static ApplicationState Instance { get; } = new ApplicationState();

    private ApplicationState() { }

    public ApplicationMode PreviousMode { get; private set; }

    private ApplicationMode _currentMode;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentModeAsString => CurrentMode.AsString();

    public ApplicationMode CurrentMode
    {
        get { return _currentMode; }
        set
        {
            if (_currentMode == value) return;

            PreviousMode = CurrentMode;
            _currentMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentModeAsString));
        }
    }

    public ProcessMetricsSnapshot? LatestTelemetry
    {
        get { return App.RuntimeState.LatestSnapshot; }
        set { App.RuntimeState.LatestSnapshot = value; NotifyTelemetryChanged(); }
    }

    public string LatestTelemetryAsString => LatestTelemetry == null ? "0" : LatestTelemetry.CpuUsage.ToString();

    public void NotifyTelemetryChanged()
    {
        OnPropertyChanged(nameof(LatestTelemetry));
        OnPropertyChanged(nameof(LatestTelemetryAsString));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}