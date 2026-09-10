using System;

namespace ProcessMonitor.Backend.Models.Errors.Collection;

public record ThreadEnumerationFailed(int ProcessId, Exception Exception) : CollectionError
{
    public override string ToString()
    {
        return $"Could not enumerate threads for PID {ProcessId}: {Exception.Message}";
    }
}