using System;

namespace ProcessMonitor.Shared.Serialization;

public interface IMessageSerializer
{
    public byte[]? Serialize<T>(T message, bool prefixed);

    public T? Deserialize<T>(byte[] data, bool prefixed);

    public Exception? GetError();
}
