using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Models.Errors.Transport.Framing;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Models.Warnings.Transport.Framing;

namespace ProcessMonitor.Shared.Transport.Framing;

public record ExactFrameReadingState(Stream Stream, CancellationToken Ct, int TotalBytesToRead, byte[]? Message = null)
{
    private const int INT32_SIZE = 4;

    public byte[] Buffer { get; init; } = Message is null ? new byte[INT32_SIZE] : Message;

    public void CleanupExplicitly() => Stream.Dispose();
}

public record FrameReadingState(Stream Stream, CancellationToken Ct)
{
    public void CleanupExplicitly() => Stream.Dispose();
}

internal interface IFrameReaderInternal : IFrameReader
{
    internal Result<ExactFrameReadingState, FramingError, FramingWarning> IsExactFrameReadingCanceled(ExactFrameReadingState state);

    internal Result<ExactFrameReadingState, FramingError, FramingWarning> IsExactFrameReadingStreamInitialized(ExactFrameReadingState state);

    internal Result<ExactFrameReadingState, FramingError, FramingWarning> IsExactFrameReadingStreamReadable(ExactFrameReadingState state);

    internal Task<Result<ExactFrameReadingState, FramingError, FramingWarning>> TryReadExactBytes(ExactFrameReadingState state);

    internal Task<Result<ExactFrameReadingState, FramingError, FramingWarning>> TryReadExactAsync(ExactFrameReadingState state);

    internal Result<FrameReadingState, FramingError, FramingWarning> IsFrameReadingCanceled(FrameReadingState state);

    internal Result<FrameReadingState, FramingError, FramingWarning> IsFrameReadingStreamInitialized(FrameReadingState state);

    internal Result<FrameReadingState, FramingError, FramingWarning> IsFrameReadingStreamReadable(FrameReadingState state);
}