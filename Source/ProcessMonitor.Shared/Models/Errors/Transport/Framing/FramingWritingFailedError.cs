using System;

namespace ProcessMonitor.Shared.Models.Errors.Transport.Framing;

public record FramingWritingFailedError(Exception Source) : FramingError
{
    public override string ToString() => $"Could not frame the input stream: {Source.Message}";
}