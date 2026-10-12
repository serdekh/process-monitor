using System;

using ProcessMonitor.Shared.Models.Errors.Transport.Framing;

namespace ProcessMonitor.Backend.Models.Errors.Transport;

public record TransportWritingError(FramingError Source) : TransportError
{
    public override string ToString() => $"Could not write to a client: {Source}";
}