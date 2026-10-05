using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Shared.Models.Warnings.Serialization;

public abstract record SerializationWarning : Warning
{
    public override string ToString() => $"Could not serialize data";
}