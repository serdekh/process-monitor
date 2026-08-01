using System.Text;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Client.Utils;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.CLient.Transport;
using ProcessMonitor.Shared.Client.Transport;
using ProcessMonitor.Shared.Transport.Framing;

using Microsoft.Extensions.Options;

namespace ProcessMonitor.Shared.Client.State;

public sealed class ClientApplicationState
{
    public ClientApplicationConfiguration Configuration { get; set; } = new();

    public StringBuilder Out { get; set; } = new StringBuilder();

    public BackendProcess Backend { get; }

    public ITransportClient CommandsPipe { get; }

    public ITransportClient TelemetryPipe { get; } 

    public CancellationToken CancellationToken { get; set; }

    public ProcessMetricsSnapshot? LatestSnapshot { get; set; }

    public ClientApplicationState(
        IFrameWriter frameWriter, 
        IFrameReader frameReader, 
        IMessageSerializer serializer,
        IOptions<ClientApplicationOptions> options)
    {
        Backend = new BackendProcess(options.Value.BackendPath);

        CommandsPipe = new TransportClient(
            ".", "ProcessMonitor.Pipes.Commands", PipeDirection.InOut, PipeOptions.Asynchronous,
            frameWriter, frameReader, Backend, serializer);

        TelemetryPipe = new TransportClient(
            ".", "ProcessMonitor.Pipes.Telemetry", PipeDirection.In, PipeOptions.Asynchronous,
            frameWriter, frameReader, Backend, serializer);

        Backend.AddOnExitHandler((sender, e) =>
        {
            _ = Task.Run(async () => 
            {
                await Backend.DisposeAsync();
                await CommandsPipe.DeinitializeAsync();
                await TelemetryPipe.DeinitializeAsync();
            });
        });
    }
}