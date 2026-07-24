using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Shared.Transport.Framing;

public interface IFrameWriter
{
    public Task<Exception?> TryWriteFrameAsync(Stream stream, byte[] message, CancellationToken ct);
}