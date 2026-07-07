using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.CLI.Common;

using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Serialization;

namespace ProcessMonitor.CLI.Transport;

public sealed class CommandPipeClient : IAsyncDisposable
{
    private NamedPipeClientStream? _client = null;

    private readonly IMessageSerializer _serializer;

    private readonly BackendProcess _backend;

    public bool IsConnected 
    { 
        get
        {
            return _backend.IsRunning && (_client?.IsConnected ?? false);
        } 
    }

    public CommandPipeClient(BackendProcess backend, IMessageSerializer serializer)
    {
        _backend = backend;  

        _backend.AddOnExitHandler(async (sender, e) =>
        {
            Console.Write("\nprocmon: warning: The backend process has terminated.\nRun the 'create' command to instantiate a new process.\nprocmon-cli>"); 
            await CleanupConnection();
        });

        _serializer = serializer;
    }
    
    public async Task CleanupConnection()
    {
        await _backend.KillAsync();
        _client?.Close();
        _client?.Dispose(); 
        _client = null;
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupConnection();
    }

    // TODO: Implement server response handling
    public async Task<bool> WriteAsync(MessageEnvelope<CommandRequest> request, CancellationToken ct)
    {
        if (_client is null)
        {
            Console.WriteLine("procmon: error: Could not write a request. No connection was established.");
            return false;
        }

        if (ct.IsCancellationRequested)
        {
            Console.WriteLine("procmon: info: Could not write a request: cancellation requested.");
            return false;
        }
 
        try 
        {
            var requestBytes = _serializer.Serialize(request);

            var lengthBytes = BitConverter.GetBytes(requestBytes.Length);

            await _client.WriteAsync(lengthBytes, 0, 4, ct);
        
            await _client.WriteAsync(requestBytes, 0, requestBytes.Length, ct);

            await _client.FlushAsync(ct);

            return true;
        }
        catch (Exception)
        {
            Console.WriteLine("procmon: error: Could not write a request to the `Commands` pipe.");
            return false;
        }
    }
 
    private bool TryCreateClientStream()
    {
        if (_client is not null) return true;

        _client = new NamedPipeClientStream(
            ".", 
            "ProcessMonitor.Pipes.Commands", 
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        if (_client is null) 
        {
            Console.WriteLine("procmon: error: Could not create the `Commands` pipe client.");
            return false;
        }

        Console.WriteLine("procmon: info: Created the `Commands` pipe client.");
        return true;
    }
   
    public async Task<bool> ConnectAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            Console.WriteLine("procmon: info: Could not start connecting to a server: cancellation requested.");
            await CleanupConnection();
            return false;
        }

        if (!_backend.Create()) 
        {
            Console.WriteLine($"procmon: error: {_backend.GetErrorString()}.");
            await CleanupConnection();
            return false; 
        }

        if (!TryCreateClientStream()) 
        {
            await CleanupConnection();
            return false;
        }

        try 
        { 
            if (_client is null) return false;

            Console.WriteLine("procmon: info: trying to connect to the `Commands` pipe.");
            
            await _client.ConnectAsync(ct);
                
            Console.WriteLine("procmon: info: Connected to the `Commands` pipe.");
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("procmon: error: Could not connect to the `Commands` pipe: access denied.");
            await CleanupConnection();
        }
        catch (InvalidOperationException) 
        {
            Console.WriteLine("procmon: error: Could not connect to `Commands` pipe: already connected.");
        }            

        return IsConnected;
    }
}
