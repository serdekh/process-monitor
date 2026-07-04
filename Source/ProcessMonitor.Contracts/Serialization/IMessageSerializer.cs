namespace ProcessMonitor.Shared.Serialization;

public interface IMessageSerializer
{
    public byte[] Serialize<T>(T message);

    public T? Deserialize<T>(byte[] data);
}
