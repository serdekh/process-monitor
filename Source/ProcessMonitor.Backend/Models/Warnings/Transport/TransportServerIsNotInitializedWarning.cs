namespace ProcessMonitor.Backend.Models.Warnings.Transport;

public record TransportServerIsNotInitializedWarning : TransportWarning
{
    public override string ToString() => $"NPFS server is not initialized";
}