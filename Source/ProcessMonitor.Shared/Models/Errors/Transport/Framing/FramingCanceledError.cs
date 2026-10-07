namespace ProcessMonitor.Shared.Models.Errors.Transport.Framing;

public record FramingCanceledError : FramingError
{
    public override string ToString() => "Failed to read frame input stream: operation is canceled";
}