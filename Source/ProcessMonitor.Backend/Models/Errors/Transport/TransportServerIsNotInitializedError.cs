namespace ProcessMonitor.Backend.Models.Errors.Transport;

public record TransportServerIsNotInitializedError : TransportError
{
    public override string ToString() => $"NPFS server is not initialized";
}