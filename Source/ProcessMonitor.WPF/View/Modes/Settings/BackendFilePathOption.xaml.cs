using ProcessMonitor.WPF.State;

using System.Windows;
using System.Windows.Controls;

namespace ProcessMonitor.WPF.View.Modes.Settings;

public partial class BackendFilePathOption : UserControl
{
    public BackendFilePathOption()
    {
        InitializeComponent();
    }

    private void BackendFilePath_PropertyValueChanged(object sender, RoutedEventArgs e)
    {
        if (sender is SettingsOption optionControl)
        {
            GlobalState.Instance.Runtime.Backend.Path = optionControl.PropertyValue;
        }
    }
}