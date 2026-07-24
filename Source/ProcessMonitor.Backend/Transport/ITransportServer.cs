using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Backend.Transport;

public interface ITransportServer
{
    public Exception? TryInitialize(
        string pipeName, 
        PipeDirection direction, 
        int maxNumberOfServerInstances, 
        PipeTransmissionMode transmissionMode,
        PipeOptions options);

    public Task<Exception?> TryConnectAsync(CancellationToken ct);

    public Task<Exception?> TryWriteAsync(byte[] message, CancellationToken ct);

    public Task<(byte[], Exception?)> TryReadAsync(CancellationToken ct);
    
    public Task DeinitializeAsync();
}