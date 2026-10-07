using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Models.Errors.Transport.Framing;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Models.Warnings.Transport.Framing;

namespace ProcessMonitor.Shared.Transport.Framing;

public sealed class FrameReader : IFrameReaderInternal
{
    public Result<ExactFrameReadingState, FramingError, FramingWarning> IsExactFrameReadingCanceled(ExactFrameReadingState state) =>
        state.Ct.IsCancellationRequested
            ? Result.Failure<ExactFrameReadingState, FramingError, FramingWarning>(new FramingCanceledError())
            : Result.Success<ExactFrameReadingState, FramingError, FramingWarning>(state);

    public Result<ExactFrameReadingState, FramingError, FramingWarning> IsExactFrameReadingStreamInitialized(ExactFrameReadingState state) =>
        state.Stream is null
            ? Result.Failure<ExactFrameReadingState, FramingError, FramingWarning>(new FramingStreamIsNotInitializedError())
            : Result.Success<ExactFrameReadingState, FramingError, FramingWarning>(state);

    public Result<ExactFrameReadingState, FramingError, FramingWarning> IsExactFrameReadingStreamReadable(ExactFrameReadingState state) =>
        !state.Stream.CanRead
            ? Result.Failure<ExactFrameReadingState, FramingError, FramingWarning>(new FramingStreamDoesNotSupportReadingError())
            : Result.Success<ExactFrameReadingState, FramingError, FramingWarning>(state);

    public Result<FrameReadingState, FramingError, FramingWarning> IsFrameReadingCanceled(FrameReadingState state) =>
        state.Ct.IsCancellationRequested
            ? Result.Failure<FrameReadingState, FramingError, FramingWarning>(new FramingCanceledError())
            : Result.Success<FrameReadingState, FramingError, FramingWarning>(state);

    public Result<FrameReadingState, FramingError, FramingWarning> IsFrameReadingStreamInitialized(FrameReadingState state) =>
        state.Stream is null
            ? Result.Failure<FrameReadingState, FramingError, FramingWarning>(new FramingStreamIsNotInitializedError())
            : Result.Success<FrameReadingState, FramingError, FramingWarning>(state);

    public Result<FrameReadingState, FramingError, FramingWarning> IsFrameReadingStreamReadable(FrameReadingState state) =>
        !state.Stream.CanRead
            ? Result.Failure<FrameReadingState, FramingError, FramingWarning>(new FramingStreamDoesNotSupportReadingError())
            : Result.Success<FrameReadingState, FramingError, FramingWarning>(state);

    public async Task<Result<ExactFrameReadingState, FramingError, FramingWarning>> TryReadExactBytes(ExactFrameReadingState state)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < state.TotalBytesToRead)
        {
            int bytesLeft = state.TotalBytesToRead - totalBytesRead;

            try
            {
                int bytesRead = await state.Stream.ReadAsync(state.Buffer.AsMemory(totalBytesRead, bytesLeft), state.Ct);

                if (bytesRead == 0)
                {
                    return Result.Success<ExactFrameReadingState, FramingError, FramingWarning>(state);
                }

                totalBytesRead += bytesRead;
            }
            catch (Exception ex)
            {
                return Result.Failure<ExactFrameReadingState, FramingError, FramingWarning>(new FramingReadingFailedError(ex));
            }
        }

        return Result.Success<ExactFrameReadingState, FramingError, FramingWarning>(state);
    }

    public async Task<Result<ExactFrameReadingState, FramingError, FramingWarning>> TryReadExactAsync(ExactFrameReadingState state)
    {
        var exactFrameReadingResult = await IsExactFrameReadingCanceled(state)
            .Bind(IsExactFrameReadingStreamInitialized)
            .Bind(IsExactFrameReadingStreamReadable)
            .BindAsync(TryReadExactBytes);

        return exactFrameReadingResult.IsSuccess()
            ? Result.Success<ExactFrameReadingState, FramingError, FramingWarning>(state)
            : Result.Failure<ExactFrameReadingState, FramingError, FramingWarning>(exactFrameReadingResult.AsFailure().Chain.Error);
    }

    private async Task<Result<ExactFrameReadingState, FramingError, FramingWarning>> TryReadPrefix(FrameReadingState state)
    {
        var prefixReadingState = new ExactFrameReadingState(state.Stream, state.Ct, 4);

        var prefixReadingResult = await TryReadExactAsync(prefixReadingState);

        return prefixReadingResult.IsFailure()
            ? Result.Failure<ExactFrameReadingState, FramingError, FramingWarning>(prefixReadingResult.AsFailure().Chain.Error)
            : Result.Success<ExactFrameReadingState, FramingError, FramingWarning>(prefixReadingState);
    }

    // TODO: Finish moving the implementation towards being functional
    public async Task<Result<byte[], FramingError, FramingWarning>> TryReadFrameAsync(Stream stream, CancellationToken ct)
    {
        var frameReadingData = new FrameReadingState(stream, ct);

        var frameReadingResult = IsFrameReadingCanceled(frameReadingData)
            .Bind(IsFrameReadingStreamInitialized)
            .Bind(IsFrameReadingStreamReadable);

        if (frameReadingResult.IsFailure())
        {
            return Result.Failure<byte[], FramingError, FramingWarning>(frameReadingResult.AsFailure().Chain.Error);
        }

        var prefixReadingResult = await TryReadPrefix(frameReadingData);

        if (prefixReadingResult.IsFailure())
        {
            return Result.Failure<byte[], FramingError, FramingWarning>(prefixReadingResult.AsFailure().Chain.Error);
        }

        var prefixBuffer = prefixReadingResult.AsSuccess().Value.Buffer;

        int length;

        try
        {
            length = BitConverter.ToInt32(prefixBuffer, startIndex: 0);
        }
        catch
        {
            return Result.Failure<byte[], FramingError, FramingWarning>(new FramingCorruptedMessagePrefixError(prefixBuffer));
        }

        if (length <= 0) return Result.Failure<byte[], FramingError, FramingWarning>(new FramingInvalidMessagePrefixError(length));

        var message = new byte[length];
        var messageReadingState = new ExactFrameReadingState(stream, ct, length, message);

        var messageReadingResult = await TryReadExactAsync(messageReadingState);

        return messageReadingResult.IsFailure()
            ? Result.Failure<byte[], FramingError, FramingWarning>(messageReadingResult.AsFailure().Chain.Error)
            : Result.Success<byte[], FramingError, FramingWarning>(message);
    }
}