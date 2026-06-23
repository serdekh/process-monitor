using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Backend.Transport;

public class CommandTransport : ICommandTransport
{
    public async Task<byte[]> ReceiveAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            // TODO: Log operation cancellation
        }

        await Task.Delay(1000, ct);
        var bytes = new byte[1] {123};

        return bytes; 
    }

    public Task SendAsync(byte[] data, CancellationToken ct)
    { 
        if (ct.IsCancellationRequested)
        {
            // TODO: Log operation cancellation
            return Task.CompletedTask;
        }

        // TODO: Replace hardcoded log with proper logic
        Console.WriteLine($"Sent {data.Length} bytes");
        return Task.CompletedTask;
    }
}
