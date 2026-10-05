using System;
using System.Collections.Generic;
using System.Text.Json;

using ProcessMonitor.Shared.Models.Errors.Serialization;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Models.Warnings.Serialization;

namespace ProcessMonitor.Shared.Serialization;

public sealed class JsonMessageSerializer : IMessageSerializer
{
    public Result<byte[], SerializationError, SerializationWarning> TrySerialize<T>(T message)
    {
        byte[] messageBytes;

        try
        {
            messageBytes = JsonSerializer.SerializeToUtf8Bytes(message);
            return new Success<byte[], SerializationError, SerializationWarning>(messageBytes);
        }
        catch (Exception ex)
        {
            return new Failure<byte[], SerializationError, SerializationWarning>(
                new ErrorChain<SerializationError>(
                    new SerializationNotSupportedError(ex)));
        }
    }

    public Result<T?, SerializationError, SerializationWarning> TryDeserialize<T>(byte[] message)
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(message);

            IReadOnlyList<SerializationWarning> warnings = result is null ? [new SerializationJsonIsNullWarning()] : [];

            return new Success<T?, SerializationError, SerializationWarning>(result) { Warnings = warnings };
        }
        catch (Exception ex)
        {
            return new Failure<T?, SerializationError, SerializationWarning>(
                new ErrorChain<SerializationError>(
                    new DeserializationError<T>(ex)));
        }
    }
}