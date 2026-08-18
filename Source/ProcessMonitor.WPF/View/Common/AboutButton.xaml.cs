using System.Windows;
using System.Windows.Controls;

namespace ProcessMonitor.WPF.View.Common;

public partial class AboutButton : UserControl
{
    public AboutButton()
    {
        InitializeComponent();
    }

    // TODO: Add about-message dispatching to show a corresponding info
    //       depending on the current mode
    private void AboutButtonControl_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        MessageBox.Show("Test: info message block", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}