using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Shared.Transport;

//TODO (not urgent but I believe it should be): Split this interface into 
// IComanndPipeClient and ICommandPipeServer and replace the old code in
// the Transport layer classes
public interface ICommandTransport
{
    Task<byte[]?> ReceiveAsync(CancellationToken ct);

    Task SendAsync(byte[] data, CancellationToken ct);

    Task InitializeAsync(CancellationToken ct);

    void Deinitialize();
}
