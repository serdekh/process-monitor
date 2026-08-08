using System.Windows.Controls;
using WPF_Experimental.Client.State;

namespace WPF_Experimental.View.Modes;

public partial class StartupModeWindow : UserControl
{
    public StartupModeWindow()
    {
        InitializeComponent();
    }

    private void RunButton_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        ApplicationState.Instance.CurrentMode = ApplicationMode.Running;
    }
}