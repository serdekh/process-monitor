using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Shared.Transport.Framing;

public interface IFrameReader
{
    public Task<(byte[], Exception?)> TryReadFrameAsync(Stream stream, CancellationToken ct);
}