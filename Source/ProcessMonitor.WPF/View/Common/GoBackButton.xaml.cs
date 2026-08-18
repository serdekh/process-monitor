using ProcessMonitor.WPF.State;

using System.Windows.Controls;

namespace ProcessMonitor.WPF.View.Common;

public partial class GoBackButton : UserControl
{
    public GoBackButton()
    {
        InitializeComponent();
    }

    private void GoBackButtonControl_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        GlobalState.Instance.CurrentMode = GlobalState.Instance.PreviousMode;
    }
}