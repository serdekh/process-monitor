using System.Text.Json;

namespace ProcessMonitor.Shared.Serialization;

public sealed class JsonMessageSerializer : IMessageSerializer
{
    public byte[] Serialize<T>(T message)
    {
        return JsonSerializer.SerializeToUtf8Bytes<T>(message);
    }

    public T? Deserialize<T>(byte[] data)
    {
        return JsonSerializer.Deserialize<T>(data);
    }
}