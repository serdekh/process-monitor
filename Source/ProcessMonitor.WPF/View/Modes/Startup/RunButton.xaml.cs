using ProcessMonitor.WPF.State;

using System.Windows;
using System.Windows.Controls;

namespace ProcessMonitor.WPF.View.Modes.Startup;

public partial class RunButton : UserControl
{
    public RunButton()
    {
        InitializeComponent();
    }

    private void RunButtonControl_Click(object sender, RoutedEventArgs e)
    {
        GlobalState.Instance.CurrentMode = ModeState.Running;
    }
}