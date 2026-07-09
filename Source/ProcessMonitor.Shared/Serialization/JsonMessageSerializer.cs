using System;
using System.Text.Json;
using System.Buffers.Binary;

namespace ProcessMonitor.Shared.Serialization;

public sealed class JsonMessageSerializer : IMessageSerializer
{
    private Exception? _error = null;

    // TODO: Replace this error handling with a different one to be thread safe
    public Exception? GetError() => _error;

    public byte[]? Serialize<T>(T message, bool prefixed)
    {
        try
        {
            byte[] messageBytes = JsonSerializer.SerializeToUtf8Bytes(message);

            if (!prefixed) return messageBytes;

            byte[] combined = new byte[4 + messageBytes.Length];
            Span<byte> combinedSpan = combined;

            BinaryPrimitives.WriteInt32BigEndian(combinedSpan[..4], messageBytes.Length);
            
            messageBytes.CopyTo(combinedSpan[4..]);
            return combined;
        }
        catch (Exception ex)
        {
            _error = ex;
            return null;
        }
    }

    public T? Deserialize<T>(byte[] message, bool prefixed)
    {
        if (!prefixed)
        {
            try
            {
                T? result = JsonSerializer.Deserialize<T>(message);
                return result;
            }
            catch (Exception ex)
            {
                _error = ex;
                return default;
            }
        }

        if (message.Length < 4)
        {
            _error = new ArgumentException("Message is too short to contain a length prefix.", nameof(message));
            return default;
        }

        try
        {
            ReadOnlySpan<byte> messageSpan = message;
            
            int messageLength = BinaryPrimitives.ReadInt32BigEndian(messageSpan[..4]);

            if (messageSpan.Length - 4 != messageLength)
            {
                _error = new InvalidOperationException($"Payload size mismatch. Expected: {messageLength} bytes, Got: {messageSpan.Length - 4} bytes.");
                return default;
            }

            ReadOnlySpan<byte> payload = messageSpan.Slice(4, messageLength);
            
            T? result = JsonSerializer.Deserialize<T>(payload);
            return result;
        }
        catch (Exception ex)
        {
            _error = ex;
            return default;
        }
    }
}