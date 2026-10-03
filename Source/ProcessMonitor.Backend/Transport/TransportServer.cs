using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.Models.Errors.Transport;
using ProcessMonitor.Backend.Models.Warnings.Transport;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Transport.Framing;

namespace ProcessMonitor.Backend.Transport;

public sealed class TransportServer : ITransportServer
{
    private NamedPipeServerStream? _server = null;

    private readonly FrameReader _frameReader;

    private readonly FrameWriter _frameWriter;

    public TransportServer()
    {
        _frameReader = new FrameReader();
        _frameWriter = new FrameWriter();
    }

    public TransportServer(
        string pipeName,
        PipeDirection direction,
        int maxNumberOfServerInstances,
        PipeTransmissionMode transmissionMode,
        PipeOptions options) : this()
    {
        var serverOptions = new TransportServerOptions(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options);
        TryInitialize(serverOptions);
    }

    public Result<None, TransportError, TransportWarning> TryInitialize(TransportServerOptions options)
    {
        try
        {
            _server = new NamedPipeServerStream(
                options.PipeName,
                options.Direction,
                options.MaxNumberOfServerInstances,
                options.Mode,
                options.Options);

            return new Success<None, TransportError, TransportWarning>(new None());
        }
        catch (Exception ex)
        {
            return new Failure<None, TransportError, TransportWarning>(
                new ErrorChain<TransportError>(
                    new TransportInitializationError(ex)));
        }
    }

    public async Task<Result<None, TransportError, TransportWarning>> TryConnectAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            return new Success<None, TransportError, TransportWarning>(new None())
            {
                Warnings = [new TransportCanceledWarning()]
            };
        }

        if (_server is null)
        {
            return new Failure<None, TransportError, TransportWarning>(
                new ErrorChain<TransportError>(
                    new TransportServerIsNotInitializedError()));
        }

        try
        {
            await _server.WaitForConnectionAsync(ct);
            return new Success<None, TransportError, TransportWarning>(new None());
        }
        catch (Exception ex)
        {
            return new Failure<None, TransportError, TransportWarning>(
                new ErrorChain<TransportError>(
                    new TransportConnectionError(ex)));
        }
    }

    public async Task<Result<None, TransportError, TransportWarning>> TryWriteAsync(byte[] message, CancellationToken ct)
    {
        if (_server is null)
        {
            return new Failure<None, TransportError, TransportWarning>(
                new ErrorChain<TransportError>(
                    new TransportServerIsNotInitializedError()));
        }

        var frameWritingException = await _frameWriter.TryWriteFrameAsync(_server, message, ct);

        return frameWritingException is null
            ? new Success<None, TransportError, TransportWarning>(new None())
            : new Failure<None, TransportError, TransportWarning>(
                new ErrorChain<TransportError>(
                    new TransportWritingError(frameWritingException)));
    }

    public async Task<Result<byte[], TransportError, TransportWarning>> TryReadAsync(CancellationToken ct)
    {
        if (_server is null)
        {
            return new Failure<byte[], TransportError, TransportWarning>(
                new ErrorChain<TransportError>(
                    new TransportServerIsNotInitializedError()));
        }

        var (bytes, frameReadingException) = await _frameReader.TryReadFrameAsync(_server, ct);

        return frameReadingException is null
            ? new Success<byte[], TransportError, TransportWarning>(bytes)
            : new Failure<byte[], TransportError, TransportWarning>(
                new ErrorChain<TransportError>(
                    new TransportWritingError(frameReadingException)));
    }

    public async Task<Success<None, TransportError, TransportWarning>> DeinitializeAsync()
    {
        if (_server is null) return new Success<None, TransportError, TransportWarning>(new None())
        {
            Warnings = [new TransportServerIsNotInitializedWarning()]
        };

        try
        {
            _server.Disconnect();

            _server.Close();

            await _server.DisposeAsync();

            _server = null;
        }
        catch { }

        return new Success<None, TransportError, TransportWarning>(new None());
    }
}