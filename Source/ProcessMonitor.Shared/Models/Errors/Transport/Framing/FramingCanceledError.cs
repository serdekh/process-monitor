namespace ProcessMonitor.Shared.Models.Errors.Transport.Framing;

public record FramingCanceledError : FramingError
{
    public override string ToString() => "Could not read/write a frame: operation is canceled";
}