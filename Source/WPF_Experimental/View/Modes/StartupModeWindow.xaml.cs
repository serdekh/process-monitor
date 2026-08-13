using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Snapshots;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using WPF_Experimental.Client.State;

namespace WPF_Experimental.View.Modes;

public partial class StartupModeWindow : UserControl
{
    public StartupModeWindow()
    {
        InitializeComponent();
    }

    private async Task<Exception?> TryInit(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new OperationCanceledException();

        var backendCreationException = App.RuntimeState.Backend.TryCreate();

        // TODO: Implement binding for the logging footer
        if (backendCreationException != null) return backendCreationException;

        var commandsPipeException = App.RuntimeState.CommandsPipe.TryInitialize();

        if (commandsPipeException != null)
        {
            await App.RuntimeState.Backend.KillAsync();
            return commandsPipeException;
        }

        var telemetryPipeException = App.RuntimeState.TelemetryPipe.TryInitialize();

        if (telemetryPipeException != null)
        {
            await App.RuntimeState.CommandsPipe.DeinitializeAsync();
            await App.RuntimeState.Backend.KillAsync();
            return telemetryPipeException;
        }

        return null;
    }

    private async Task<Exception?> TryConnect(CancellationToken ct)
    {
        var commandsPipeException = await App.RuntimeState.CommandsPipe.TryConnectAsync(ct);

        if (commandsPipeException != null)
        {
            await App.RuntimeState.Backend.KillAsync();
            return commandsPipeException;
        }

        var telemetryPipeException = await App.RuntimeState.TelemetryPipe.TryConnectAsync(ct);

        if (telemetryPipeException != null)
        {
            await App.RuntimeState.CommandsPipe.DeinitializeAsync();
            await App.RuntimeState.Backend.KillAsync();
            return telemetryPipeException;
        }

        return null;
    }

    private async Task<Exception?> TryCreateAndConnect(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new OperationCanceledException();

        var initException = await TryInit(ct);

        if (initException != null) return initException;

        return await TryConnect(ct);
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        ApplicationState.Instance.CurrentMode = ApplicationMode.Running;

        var ct = new CancellationTokenSource().Token;

        var ex = await TryCreateAndConnect(ct);

        if (ex != null)
        {
            ApplicationState.Instance.CurrentMode = ApplicationMode.Startup;
            return;
        }

        var res = await App.RuntimeState.CommandsPipe.TryWriteAsync(new MessageEnvelope<CommandRequest>()
        {
            Type = MessageType.CommandRequest,
            Payload = new CommandRequest()
            {
                Method = "post",
                Route = "monitoring",
                Body = JsonSerializer.SerializeToElement(new { version = 0.1, requestId = 0, pid = 0 })
            }
        }, ct);

        if (res != null)
        {
            ApplicationState.Instance.CurrentMode = ApplicationMode.Startup;
            return;
        }

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                (var latest, var telemetryReadingException) = await App.RuntimeState.TelemetryPipe.TryReadAsync<ProcessMetricsSnapshot>(ct);
    
                if (telemetryReadingException is not null)
                {
                    ApplicationState.Instance.CurrentMode = ApplicationMode.Startup;
                    return;
                }

                if (latest != null)
                {
                    ApplicationState.Instance.LatestTelemetry = latest.Payload;
                    ApplicationState.Instance.NotifyTelemetryChanged();
                }

                try
                {
                    await Task.Delay(50, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, ct);

    }
}