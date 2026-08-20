using ProcessMonitor.WPF.Services;
using ProcessMonitor.WPF.State;

using System.Windows.Controls;

namespace ProcessMonitor.WPF.View.Modes.Settings;

public partial class TargetPIDOption : UserControl
{
    public TargetPIDOption()
    {
        InitializeComponent();
    }

    private void TargetProcessId_PropertyValueChanged(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is not SettingsOption optionControl) return;
        
        var rawProcessId = optionControl.PropertyValue;

        try
        {
            var processId = int.Parse(rawProcessId);

            GlobalState.Instance.Configuration.ProcessId = processId;
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Log($"[Error]: Could not convert input process id: {ex.Message}");
        }
    }
}