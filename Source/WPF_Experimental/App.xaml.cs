using System.Windows;
using Microsoft.Extensions.Options;
using ProcessMonitor.Shared.Client.Input.Args;
using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Transport.Framing;

namespace WPF_Experimental;

public partial class App : Application
{
    private static readonly ClientApplicationState _runtimeState = new
    (
        new FrameWriter(),
        new FrameReader(),
        new JsonMessageSerializer(),
        Options.Create(new ClientApplicationConfiguration())
    );

    public static ClientApplicationState RuntimeState => _runtimeState;

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

        RuntimeState.Backend.Path = argsParser.Configuration.ServerFilepath;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        RuntimeState.Cleanup();
    }
}
