using System;
using System.Text.Json;

namespace ProcessMonitor.Shared.Serialization;

public sealed class JsonMessageSerializer : IMessageSerializer
{
    public (byte[], Exception?) TrySerialize<T>(T message)
    {   
        byte[] messageBytes;
        
        try
        {
            messageBytes = JsonSerializer.SerializeToUtf8Bytes(message);
            return (messageBytes, null);
        }
        catch (Exception ex)
        {
            return (Array.Empty<byte>(), ex);
        }
    }

    public (T?, Exception?) TryDeserialize<T>(byte[] message)
    {
        try
        {
            var result = JsonSerializer.Deserialize<T>(message);

            if (result is null)
            {
                return (default, new InvalidOperationException("Message is corrupted"));    
            }

            return (result, null);
        }
        catch (Exception ex)
        {
            return (default, ex);
        }
    }
}