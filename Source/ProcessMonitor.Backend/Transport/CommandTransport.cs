using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace ProcessMonitor.Backend.Transport;

public class CommandTransport : ICommandTransport
{
    private readonly NamedPipeServerStream _pipe;
    
    private IPCProtocolReader _reader;
    private IPCProtocolWriter _writer;

    public CommandTransport()
    {
        _pipe = new NamedPipeServerStream(
            pipeName:                   "ProcessMonitor.Pipes.Commands",
            direction:                  PipeDirection.InOut,
            maxNumberOfServerInstances: 1,
            transmissionMode:           PipeTransmissionMode.Byte,
            options:                    PipeOptions.Asynchronous);

        _reader = new IPCProtocolReader(_pipe);
        _writer = new IPCProtocolWriter(_pipe);
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        await _pipe.WaitForConnectionAsync();
    }

    public async Task<byte[]?> ReceiveAsync(CancellationToken ct)
    {
        return await _reader.ReadAsync(ct);
    }

    public async Task SendAsync(byte[] data, CancellationToken ct)
    {
        await _writer.WriteAsync(data, ct);
    }
}
