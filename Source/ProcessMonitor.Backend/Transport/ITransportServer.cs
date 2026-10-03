using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.Models.Errors.Transport;
using ProcessMonitor.Backend.Models.Warnings.Transport;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Transport;

public interface ITransportServer
{
    public Result<None, TransportError, TransportWarning> TryInitialize(TransportServerOptions options);

    public Task<Result<None, TransportError, TransportWarning>> TryConnectAsync(CancellationToken ct);

    public Task<Result<None, TransportError, TransportWarning>> TryWriteAsync(byte[] message, CancellationToken ct);

    public Task<Result<byte[], TransportError, TransportWarning>> TryReadAsync(CancellationToken ct);

    public Task<Success<None, TransportError, TransportWarning>> DeinitializeAsync();
}