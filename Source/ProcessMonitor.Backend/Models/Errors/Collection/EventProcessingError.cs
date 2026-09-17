using System;

namespace ProcessMonitor.Backend.Models.Errors.Collection;

public record EventProcessingError(Exception Exception) : CollectionError
{
    public override string ToString()
    {
        return $"Could not process events: {Exception.Message}";
    }
}