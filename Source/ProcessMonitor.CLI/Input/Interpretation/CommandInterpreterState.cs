using System.Text;
using System.IO.Pipes;
using System.Threading;
using System.Collections.Generic;

using ProcessMonitor.CLI.Common;
using ProcessMonitor.CLI.Transport;

using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Transport.Framing;

namespace ProcessMonitor.CLI.Input.Interpretation;

public sealed class CommandInterpreterState
{
    public int? ProcessId { get; set; } = null;

    public StringBuilder Out { get; set; } = new StringBuilder();

    public BackendProcess Backend { get; set; }

    public ITransportClient CommandsPipe { get; set; }

    public ITransportClient TelemetryPipe { get; set; } 

    public CancellationToken CancellationToken { get; set; }

    public List<ProcessMetricsSnapshot> Telemetry { get; set; } = new();

    public CommandInterpreterState(
        BackendProcess backendProcess,
        IFrameWriter frameWriter,
        IFrameReader frameReader,
        IMessageSerializer serializer)
    {
        Backend = backendProcess;

        CommandsPipe = new TransportClient(
            ".", "ProcessMonitor.Pipes.Commands", PipeDirection.InOut, PipeOptions.Asynchronous,
            frameWriter, frameReader, backendProcess, serializer);

        TelemetryPipe = new TransportClient(
            ".", "ProcessMonitor.Pipes.Telemetry", PipeDirection.In, PipeOptions.Asynchronous,
            frameWriter, frameReader, backendProcess, serializer);

        Backend.AddOnExitHandler(async (sender, e) =>
        {
            await Backend.DisposeAsync();
            await CommandsPipe.DeinitializeAsync();
            await TelemetryPipe.DeinitializeAsync();
        });
    }  
}