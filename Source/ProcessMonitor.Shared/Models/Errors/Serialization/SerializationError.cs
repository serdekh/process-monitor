using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Shared.Models.Errors.Serialization;

public abstract record SerializationError : Error
{
    public override string ToString() => $"Could not serialize data";
}