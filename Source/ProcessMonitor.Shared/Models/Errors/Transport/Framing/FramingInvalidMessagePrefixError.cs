namespace ProcessMonitor.Shared.Models.Errors.Transport.Framing;

public record FramingInvalidMessagePrefixError(int Source) : FramingError
{
    public override string ToString() => $"Message prefix has invalid value: {Source}";
}