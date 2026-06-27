using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Backend.Transport;

public sealed class IPCProtocolReader
{
    private readonly Stream _stream;

    public IPCProtocolReader(Stream stream)
    {
        _stream = stream;
    }

    // TODO: Inspect the method for possible convertion errors,
    // invalid data boundaries and so on
    // Rewrite it to make byte[] array nullable for the error cases
    public async Task<byte[]?> ReadAsync(CancellationToken ct)
    {
        try
        {    
            var lengthBuffer = new byte[4];

            if (!await ReadExactAsync(lengthBuffer, 4, ct))
            {
                return null;
            }

            var length = BitConverter.ToInt32(lengthBuffer, 0);

            if (length <= 0)
            {
                return null;
            }

            var buffer = new byte[length];

            if (!await ReadExactAsync(buffer, length, ct))
            {
                return null;
            }

            return buffer;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async Task<bool> ReadExactAsync(byte[] buffer, int totalBytesToRead, CancellationToken ct)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < totalBytesToRead)
        {
            int bytesLeft = totalBytesToRead - totalBytesRead;
            
            int bytesRead = await _stream.ReadAsync(buffer, totalBytesRead, bytesLeft, ct);

            if (bytesRead == 0)
            {
                return false; 
            }

            totalBytesRead += bytesRead;
        }

        return true;
    }
}
