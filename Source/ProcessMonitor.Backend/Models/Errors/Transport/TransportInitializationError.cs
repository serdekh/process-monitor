using System;

namespace ProcessMonitor.Backend.Models.Errors.Transport;

public record TransportInitializationError(Exception Source) : TransportError
{
    public override string ToString() => $"Could not initialize NPFS server: {Source.Message}";
}