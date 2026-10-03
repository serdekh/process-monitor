namespace ProcessMonitor.Backend.Models.Warnings.Transport;

public record TransportCanceledWarning : TransportWarning
{
    public override string ToString() => $"Operation is canceled";
}