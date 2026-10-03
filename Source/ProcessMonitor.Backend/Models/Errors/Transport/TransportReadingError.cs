using System;

namespace ProcessMonitor.Backend.Models.Errors.Transport;

public record TransportReadingError(Exception Source) : TransportError
{
    public override string ToString() => $"Could not read from a client: {Source.Message}";
}