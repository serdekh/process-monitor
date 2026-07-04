using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Shared.Transport;

public interface ICommandTransport
{
    Task<byte[]?> ReceiveAsync(CancellationToken ct);

    Task SendAsync(byte[] data, CancellationToken ct);

    Task InitializeAsync(CancellationToken ct);

    void Deinitialize();
}
