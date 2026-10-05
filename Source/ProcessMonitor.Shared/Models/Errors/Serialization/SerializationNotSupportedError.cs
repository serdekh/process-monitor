using System;

namespace ProcessMonitor.Shared.Models.Errors.Serialization;

public record SerializationNotSupportedError(Exception Source) : SerializationError
{
    public override string ToString() => $"Input data cannot be serialized: {Source.Message}";
}