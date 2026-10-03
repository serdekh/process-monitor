using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Models.Warnings.Transport;

public abstract record TransportWarning : Warning
{
    public override string ToString() => $"NPFS server warning";
}