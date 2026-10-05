using ProcessMonitor.Shared.Models.Errors.Serialization;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Models.Warnings.Serialization;

namespace ProcessMonitor.Shared.Serialization;

public interface IMessageSerializer
{
    public Result<byte[], SerializationError, SerializationWarning> TrySerialize<T>(T message);

    public Result<T?, SerializationError, SerializationWarning> TryDeserialize<T>(byte[] message);
}