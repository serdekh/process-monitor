using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Errors.Transport.Framing;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Models.Warnings.Transport.Framing;

namespace ProcessMonitor.Shared.Transport.Framing;

public interface IFrameWriter
{
    public Task<Result<None, FramingError, FramingWarning>> TryWriteFrameAsync(Stream stream, byte[] message, CancellationToken ct);
}