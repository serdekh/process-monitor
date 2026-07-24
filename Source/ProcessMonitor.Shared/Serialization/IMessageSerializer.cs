using System;

namespace ProcessMonitor.Shared.Serialization;

public interface IMessageSerializer
{
    public (byte[], Exception?) TrySerialize<T>(T message);

    public (T?, Exception?) TryDeserialize<T>(byte[] message);
}
