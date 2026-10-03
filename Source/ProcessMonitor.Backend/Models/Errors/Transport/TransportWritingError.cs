using System;

namespace ProcessMonitor.Backend.Models.Errors.Transport;

public record TransportWritingError(Exception Source) : TransportError
{
    public override string ToString() => $"Could not write to a client: {Source.Message}";
}