using System.Windows;
using Microsoft.Extensions.Options;
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
        // TODO: Unhardcode these constants via the settings mode configurations
        Options.Create(new ClientApplicationConfiguration 
        { 
            ProcessId = 0, 
            ServerFilepath = "\"C:\\Users\\Serhii\\repos\\process-monitor\\Source\\ProcessMonitor.Backend\\bin\\Debug\\net9.0\\ProcessMonitor.Backend.exe\""
        })
    );

    public static ClientApplicationState RuntimeState => _runtimeState;
}
