using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Errors.Transport.Framing;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Models.Warnings.Transport.Framing;

namespace ProcessMonitor.Shared.Transport.Framing;

public sealed class FrameWriter : IFrameWriterInternal
{
    public Result<FrameWritingContext, FramingError, FramingWarning> IsWritingCanceled(FrameWritingContext context) =>
        context.Ct.IsCancellationRequested
            ? Result.Failure<FrameWritingContext, FramingError, FramingWarning>(new FramingCanceledError())
            : Result.Success<FrameWritingContext, FramingError, FramingWarning>(context);

    public Result<FrameWritingContext, FramingError, FramingWarning> IsStreamInitialized(FrameWritingContext context) =>
        context.Stream is null
            ? Result.Failure<FrameWritingContext, FramingError, FramingWarning>(new FramingStreamIsNotInitializedError())
            : Result.Success<FrameWritingContext, FramingError, FramingWarning>(context);

    public Result<FrameWritingContext, FramingError, FramingWarning> IsStreamWriteable(FrameWritingContext context) =>
        !context.Stream.CanWrite
            ? Result.Failure<FrameWritingContext, FramingError, FramingWarning>(new FramingReadonlyStreamError())
            : Result.Success<FrameWritingContext, FramingError, FramingWarning>(context);

    public async Task<Result<None, FramingError, FramingWarning>> TryWriteBytesAsync(FrameWritingContext context)
    {
        try
        {
            var messageLength = BitConverter.GetBytes(context.Message.Length);

            await context.Stream.WriteAsync(messageLength, context.Ct);

            await context.Stream.WriteAsync(context.Message, context.Ct);

            await context.Stream.FlushAsync(context.Ct);

            return Result.Success<None, FramingError, FramingWarning>(None.New());
        }
        catch (Exception ex)
        {
            return Result.Failure<None, FramingError, FramingWarning>(new FramingWritingFailedError(ex));
        }
    }

    public async Task<Result<None, FramingError, FramingWarning>> TryWriteFrameAsync(Stream stream, byte[] message, CancellationToken ct)
    {
        var context = new FrameWritingContext(stream, message, ct);

        return await IsWritingCanceled(context)
            .Bind(IsStreamInitialized)
            .Bind(IsStreamWriteable)
            .BindAsync(TryWriteBytesAsync);
    }
}