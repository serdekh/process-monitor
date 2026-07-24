using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.CLI.Common;

using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Transport.Framing;

namespace ProcessMonitor.CLI.Transport;

public sealed class TransportClient : ITransportClient, IAsyncDisposable
{
    private NamedPipeClientStream? _client;

    private readonly string _serverName;

    private readonly string _pipeName;

    private readonly PipeDirection _pipeDirection;

    private readonly PipeOptions _pipeOptions;

    private readonly IFrameWriter _frameWriter;

    private readonly IFrameReader _frameReader;

    private readonly BackendProcess _backendProcess;

    private readonly IMessageSerializer _serializer;

    public TransportClient(
        string serverName, 
        string pipeName, 
        PipeDirection direction, 
        PipeOptions options,
        IFrameWriter frameWriter,
        IFrameReader frameReader,
        BackendProcess backendProcess,
        IMessageSerializer serializer) 
    {
        _frameWriter = frameWriter;
        _frameReader = frameReader;
        _backendProcess = backendProcess;
        _serializer = serializer;

        _serverName = serverName;
        _pipeName = pipeName;
        _pipeDirection = direction;
        _pipeOptions = options;
    }

    public bool IsConnected()
    {
        return _backendProcess.IsRunning && _client is not null && _client.IsConnected;
    }

    public Exception? TryInitialize()
    {
        if (_client is not null) return new InvalidOperationException("Client stream is already initialized");

        try
        {
            _client = new NamedPipeClientStream(_serverName, _pipeName, _pipeDirection, _pipeOptions);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public async Task<Exception?> TryConnectAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new OperationCanceledException();

        if (_client is null) return new InvalidOperationException("Client stream has not been initialized");

        if (IsConnected()) return new InvalidOperationException("Already connected");

        try
        {
            await _client.ConnectAsync(ct);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public async Task<Exception?> TryWriteAsync<T>(MessageEnvelope<T> message, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new OperationCanceledException();

        if (!IsConnected()) return new InvalidOperationException("Client is not connected");

        if (_client is null) return new InvalidOperationException("Client stream has not been initialized");

        if (_pipeDirection == PipeDirection.In) return new InvalidOperationException("Client stream only supports reading");

        byte[] messageBytes;
 
        (messageBytes, var serializationException) = _serializer.TrySerialize(message);

        if (serializationException is not null) return serializationException;
        
        var writingException = await _frameWriter.TryWriteFrameAsync(_client, messageBytes, ct);

        if (writingException is not null) return writingException;
        
        return null;
    }

    public async Task<(MessageEnvelope<T>, Exception?)> TryReadAsync<T>(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return (new MessageEnvelope<T>(), new OperationCanceledException());

        if (!IsConnected()) return (new MessageEnvelope<T>(), new InvalidOperationException("Client is not connected"));

        if (_client is null) return (new MessageEnvelope<T>(), new InvalidOperationException("Client stream has not been initialized"));

        if (_pipeDirection == PipeDirection.Out) return (new MessageEnvelope<T>(), new InvalidOperationException("Client stream only supports writing"));

        byte[] payload;

        (payload, var frameReadingException) = await _frameReader.TryReadFrameAsync(_client, ct);

        if (frameReadingException is not null) return (new MessageEnvelope<T>(), frameReadingException);

        (var envelope, var deserializationException) = _serializer.TryDeserialize<MessageEnvelope<T>>(payload);

        if (deserializationException is not null) return (new MessageEnvelope<T>(), deserializationException);

        if (envelope is null) return (new MessageEnvelope<T>(), new ArgumentException("Could not deserialize envelope"));

        return (envelope, null);
    }
    
    public async Task DeinitializeAsync()
    {
        if (_backendProcess.IsRunning)
        {
            await _backendProcess.DisposeAsync();
        }

        if (_client is not null)
        {
            _client.Close();

            await _client.DisposeAsync();

            _client = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DeinitializeAsync();
    }
}