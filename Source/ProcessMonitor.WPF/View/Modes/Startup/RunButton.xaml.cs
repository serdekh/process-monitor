using System.Windows;
using System.Windows.Controls;

using ProcessMonitor.WPF.Services;
using ProcessMonitor.WPF.State;

namespace ProcessMonitor.WPF.View.Modes.Startup;

public partial class RunButton : UserControl
{
    public RunButton()
    {
        InitializeComponent();
    }

    private async Task<Exception?> TrySwitchToRunningMode()
    {
        var runtimeInitializationException = await GlobalState.Instance.TryInitializeRuntime();

        if (runtimeInitializationException is not null) return runtimeInitializationException;

        return await GlobalState.Instance.StartProcessing();
    }

    private async void RunButtonControl_Click(object sender, RoutedEventArgs e)
    {
        var modeSwitchingException = await TrySwitchToRunningMode();

        if (modeSwitchingException is null) return;

        LoggingService.Instance.Log($"[Error]: {modeSwitchingException.Message}");
    }
}