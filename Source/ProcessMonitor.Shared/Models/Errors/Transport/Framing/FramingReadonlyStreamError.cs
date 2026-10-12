namespace ProcessMonitor.Shared.Models.Errors.Transport.Framing;

public record FramingReadonlyStreamError : FramingError
{
    public override string ToString() => $"Could not frame the input stream because it's read-only";
}