using System;

namespace ProcessMonitor.Backend.Models.Errors.Collection;

public record EventWriteFailed(Exception Exception, RawEventKind Kind) : CollectionError
{
    public override string ToString()
    {
        return $"Failed to enqueue {Kind.AsString()} event: {Exception.Message}";
    }
}