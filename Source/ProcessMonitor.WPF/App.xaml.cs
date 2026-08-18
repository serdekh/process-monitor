using ProcessMonitor.Shared.Client.Input.Args;
using ProcessMonitor.WPF.State;

using System.Windows;

namespace ProcessMonitor.WPF;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var argsParser = new ArgsParser();

        var parsingException = argsParser.Parse(e.Args);

        if (parsingException != null)
        {
            MessageBox.Show(parsingException.Message, "error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        GlobalState.Instance.Runtime.Backend.Path = argsParser.Configuration.ServerFilepath;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        GlobalState.Instance.Runtime.Cleanup();
    }
}