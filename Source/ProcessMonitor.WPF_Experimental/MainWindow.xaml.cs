using System.Windows;

namespace ProcessMonitor.WPF_Experimental;

public partial class MainWindow : Window
{
    public bool RunButtonState { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Run_Click(object sender, RoutedEventArgs e)
    {
        if (RunButtonState)
        {
            MainLabel.Text = "Stopped";
            Run.Content = "Run";
        }
        else
        {
            MainLabel.Text = "Running";
            Run.Content = "Stop";
        }

        RunButtonState = !RunButtonState;
    }
}