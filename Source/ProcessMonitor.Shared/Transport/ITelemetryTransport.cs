using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Shared.Transport;

public interface ITelemetryTransport
{
    Task SendAsync(byte[] data, CancellationToken ct);

    Task InitializeAsync(CancellationToken ct);

    void Deinitialize();
}
