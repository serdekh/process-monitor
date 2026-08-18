using System.Collections.ObjectModel;
using System.Windows;

namespace ProcessMonitor.WPF.Services;

// TODO: Define a class for a single log instead of
//       using the string type
public sealed class LoggingService
{
    public static LoggingService Instance = new();

    private LoggingService() { }

    public ObservableCollection<string> Logs { get; private set; } = [];

    public void Log(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Logs.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        });
    }
}