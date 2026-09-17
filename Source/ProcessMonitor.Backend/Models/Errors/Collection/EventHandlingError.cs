using System;

namespace ProcessMonitor.Backend.Models.Errors.Collection;

public record EventHandlingError(RawEventKind Kind) : CollectionError
{
    public override string ToString()
    {
        return $"Failed to handle {Kind.AsString()} event";
    }
}