using System.IO.Pipes;

namespace ProcessMonitor.Backend.Transport;

public record TransportServerOptions(
    string PipeName, 
    PipeDirection Direction, 
    int MaxNumberOfServerInstances, 
    PipeTransmissionMode Mode, 
    PipeOptions Options)
{
    public static TransportServerOptions CreateDefault()
    {
        return new TransportServerOptions("ProcessMonitor.Pipes.Commands", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
    }
}