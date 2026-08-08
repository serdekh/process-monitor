using ProcessMonitor.Shared.Client.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using WPF_Experimental.Client.State;

namespace WPF_Experimental.View.Common;

public partial class SettingsButton : UserControl
{
    public SettingsButton()
    {
        InitializeComponent();
    }

    private void SettingsModeButton_Click(object sender, RoutedEventArgs e)
    {
        ApplicationState.Instance.CurrentMode = ApplicationMode.Settings;
    }
}