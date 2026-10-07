using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Shared.Models.Errors.Transport.Framing;

public abstract record FramingError : Error
{
    public override string ToString() => $"Failed to frame a byte stream";
}