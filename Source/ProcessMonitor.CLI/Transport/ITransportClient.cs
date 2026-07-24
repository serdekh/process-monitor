using System;
using System.Threading;
using System.Threading.Tasks;
using ProcessMonitor.Shared.Protocol;

namespace ProcessMonitor.CLI.Transport;

public interface ITransportClient
{
    public Exception? TryInitialize();

    public bool IsConnected();

    public Task<Exception?> TryConnectAsync(CancellationToken ct);

    public Task<Exception?> TryWriteAsync<T>(MessageEnvelope<T> message, CancellationToken ct);

    public Task<(MessageEnvelope<T>, Exception?)> TryReadAsync<T>(CancellationToken ct);
    
    public Task DeinitializeAsync();
}