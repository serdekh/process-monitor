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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}