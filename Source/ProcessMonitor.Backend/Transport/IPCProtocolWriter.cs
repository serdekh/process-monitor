using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Backend.Transport;

public sealed class IPCProtocolWriter
{
    private Stream _stream;

    public IPCProtocolWriter(Stream stream)
    {
        _stream = stream;
    }

    public async Task WriteAsync(byte[] data, CancellationToken ct)
    {
        var length = BitConverter.GetBytes(data.Length);

        await _stream.WriteAsync(length, ct);
        await _stream.WriteAsync(data, ct);
        await _stream.FlushAsync(ct);
    }
}
