using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Shared.Transport;

public sealed class IPCProtocolWriter
{
    private Stream _stream;

    public IPCProtocolWriter(Stream stream)
    {
        _stream = stream;
    }

    public async Task WriteAsync(byte[] data, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;

        var length = BitConverter.GetBytes(data.Length);

        await _stream.WriteAsync(length, ct);
        await _stream.WriteAsync(data, ct);
        await _stream.FlushAsync(ct);
    }
}
