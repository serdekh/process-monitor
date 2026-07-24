using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Shared.Transport.Framing;

public sealed class FrameWriter : IFrameWriter
{
    public async Task<Exception?> TryWriteFrameAsync(Stream stream, byte[] message, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new OperationCanceledException("Cancellation requested");

        if (stream is null) return new InvalidOperationException("No stream instance was initialized");

        if (!stream.CanWrite) return new InvalidOperationException("Stream does not support writing");

        var messageLength = BitConverter.GetBytes(message.Length);

        try
        {
            await stream.WriteAsync(messageLength, ct);

            await stream.WriteAsync(message, ct);

            await stream.FlushAsync(ct);

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}