using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Shared.Transport.Framing;

internal interface IFrameReaderInternal : IFrameReader
{
    internal Task<Exception?> TryReadExactAsync(Stream stream, byte[] buffer, int totalBytesToRead, CancellationToken ct);
}