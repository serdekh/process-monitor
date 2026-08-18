using ProcessMonitor.WPF.State;

using System.Windows.Controls;

namespace ProcessMonitor.WPF.View.Common;

public partial class SettingsButton : UserControl
{
    public SettingsButton()
    {
        InitializeComponent();
    }

    private void SettingsButtonControl_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        GlobalState.Instance.CurrentMode = ModeState.Settings;
    }
}