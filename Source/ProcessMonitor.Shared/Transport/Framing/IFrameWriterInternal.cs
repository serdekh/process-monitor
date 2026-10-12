using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Errors.Transport.Framing;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Models.Warnings.Transport.Framing;

namespace ProcessMonitor.Shared.Transport.Framing;

public record FrameWritingContext(Stream Stream, byte[] Message, CancellationToken Ct);

internal interface IFrameWriterInternal : IFrameWriter
{
    internal Result<FrameWritingContext, FramingError, FramingWarning> IsWritingCanceled(FrameWritingContext context);

    internal Result<FrameWritingContext, FramingError, FramingWarning> IsStreamInitialized(FrameWritingContext context);

    internal Result<FrameWritingContext, FramingError, FramingWarning> IsStreamWriteable(FrameWritingContext context);

    internal Task<Result<None, FramingError, FramingWarning>> TryWriteBytesAsync(FrameWritingContext context);
}