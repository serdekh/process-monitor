using System.Windows;
using System.Windows.Controls;
using WPF_Experimental.Client.State;

namespace WPF_Experimental.View.Common;

public partial class UndoSwitchButton : UserControl
{
    public UndoSwitchButton()
    {
        InitializeComponent();
    }

    private void UndoSwitchModeButton_Click(object sender, RoutedEventArgs e)
    {
        ApplicationState.Instance.CurrentMode = ApplicationState.Instance.PreviousMode;
    }
}

