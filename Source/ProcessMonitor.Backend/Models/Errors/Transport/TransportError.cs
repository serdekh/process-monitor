using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Models.Errors.Transport;

public abstract record TransportError : Error
{
    public override string ToString() => $"Could not transport data via NPFS";
}