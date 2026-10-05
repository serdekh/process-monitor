using System;

namespace ProcessMonitor.Shared.Models.Errors.Serialization;

public record DeserializationError<T>(Exception Source) : SerializationError
{
    public override string ToString() => $"Input data cannot be deserialized from type {nameof(T)}: {Source.Message}";
}