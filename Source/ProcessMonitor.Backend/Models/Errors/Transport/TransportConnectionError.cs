using System;

namespace ProcessMonitor.Backend.Models.Errors.Transport;

public record TransportConnectionError(Exception Source) : TransportError
{
    public override string ToString() => $"Could not connect to a client: {Source.Message}";
}