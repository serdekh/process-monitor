namespace ProcessMonitor.Shared.Models.Errors.Transport.Framing;

public record FramingCorruptedMessagePrefixError(byte[] Source) : FramingError
{
    public override string ToString() => $"Message prefix could not be converted into a 32-bit integer: {Source}";
}