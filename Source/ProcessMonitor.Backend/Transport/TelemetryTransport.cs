using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Backend.Transport;

public class TelemetryTransport : ITelemetryTransport
{
    public Task SendAsync(byte[] data, CancellationToken ct)
    { 
        if (ct.IsCancellationRequested)
        {
            // TODO: Log operation cancellation
            return Task.CompletedTask;
        }

        // TODO: Replace hardcoded log with proper logic
        Console.WriteLine(Encoding.UTF8.GetString(data));

        return Task.CompletedTask;
    }
}
