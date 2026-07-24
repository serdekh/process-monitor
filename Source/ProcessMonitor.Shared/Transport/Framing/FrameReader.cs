using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Shared.Transport.Framing;

public sealed class FrameReader : IFrameReader
{
    private static async Task<Exception?> TryReadExactAsync(Stream stream, byte[] buffer, int totalBytesToRead, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new OperationCanceledException("Cancellation requested");
        
        if (stream is null) return new OperationCanceledException("No stream instance was initialized");

        if (!stream.CanRead) return new InvalidOperationException("Stream does not support reading");
        
        int totalBytesRead = 0;

        while (totalBytesRead < totalBytesToRead)
        {
            int bytesLeft = totalBytesToRead - totalBytesRead;
            
            try
            {
                int bytesRead = await stream.ReadAsync(buffer.AsMemory(totalBytesRead, bytesLeft), ct);

                if (bytesRead == 0) return null;

                totalBytesRead += bytesRead;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        return null;
    }

    public async Task<(byte[], Exception?)> TryReadFrameAsync(Stream stream, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return ([], new OperationCanceledException("Cancellation requested"));
        
        if (stream is null) return ([], new OperationCanceledException("No stream instance was initialized"));

        if (!stream.CanRead) return ([], new InvalidOperationException("The stream does not support reading"));

        var lengthBuffer = new byte[4];

        var prefixReadingException = await TryReadExactAsync(stream, lengthBuffer, 4, ct);

        if (prefixReadingException is not null) return ([], prefixReadingException);

        int length;

        try
        {
            length = BitConverter.ToInt32(lengthBuffer, startIndex: 0);
        }
        catch
        {
            return (Array.Empty<byte>(), new ArgumentException("Message length prefix is corrupted"));
        }

        if (length <= 0) return (Array.Empty<byte>(), new ArgumentException("Value of message length prefix is less than zero"));
        
        var message = new byte[length];

        var messageReadingException = await TryReadExactAsync(stream, message, length, ct);

        if (messageReadingException is not null) return ([], messageReadingException);

        return (message, null);
    }
}