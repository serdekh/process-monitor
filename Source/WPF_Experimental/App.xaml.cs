using System.Windows;
using ProcessMonitor.Shared.Client.Hosting;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = new ClientHostBuilder(e.Args);

        builder.UseCore();

        builder.Build();

        builder.RunAsync();
    }
}
