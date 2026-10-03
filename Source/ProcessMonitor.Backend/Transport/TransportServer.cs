using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

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

    public Exception? TryInitialize(TransportServerOptions options)
    {
        try
        {
            _server = new NamedPipeServerStream(
                options.PipeName, 
                options.Direction, 
                options.MaxNumberOfServerInstances, 
                options.Mode, 
                options.Options);

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public async Task<Exception?> TryConnectAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new OperationCanceledException("Cancellation requested");

        if (_server is null) return new InvalidOperationException("No server instance was initialized");

        try
        {
            await _server.WaitForConnectionAsync(ct);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public async Task<Exception?> TryWriteAsync(byte[] message, CancellationToken ct)
    {
        if (_server is null) return new InvalidOperationException("No server instance was initialized");

        return await _frameWriter.TryWriteFrameAsync(_server, message, ct);
    }

    public async Task<(byte[], Exception?)> TryReadAsync(CancellationToken ct)
    {
        if (_server is null) return ([], new InvalidOperationException("No server instance was initialized"));

        return await _frameReader.TryReadFrameAsync(_server, ct);
    }

    public async Task DeinitializeAsync()
    {
        if (_server is null) return;

        try
        {
            _server.Disconnect();

            _server.Close();

            await _server.DisposeAsync();

            _server = null;
        }
        catch { }
    }
}