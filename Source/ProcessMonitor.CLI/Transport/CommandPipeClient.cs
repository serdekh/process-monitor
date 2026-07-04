using System;
using System.IO.Pipes;
using System.Threading.Tasks;

using ProcessMonitor.CLI.Common;

using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Serialization;

namespace ProcessMonitor.CLI.Transport;

public sealed class CommandPipeClient : IDisposable
{
    private NamedPipeClientStream? _client = null;

    private IMessageSerializer _serializer;

    private BackendProcess _backend;

    public bool IsBackendInstanceCreated
    {
        get
        {
            return _backend.IsCreated;
        }
    }

    public bool IsConnected 
    { 
        get
        {
            return _client?.IsConnected ?? false;
        } 
    }

    // NOTE: Consider adding a hosting system similar to the backend project since
    // the serialization dependency has to be injected manually
    public CommandPipeClient(string backendFilePath, IMessageSerializer? serializer = null)
    {
        _backend = new BackendProcess(backendFilePath);       
        _serializer = serializer is null ? new JsonMessageSerializer() : serializer;
    }
    
    public void CleanupConnection()
    {
        _backend.Kill();
        _client?.Close();
        _client?.Dispose(); 
        _client = null;
    }

    public void Dispose()
    {
        CleanupConnection();
    }

    public async Task WriteAsync(CommandRequest request)
    {
        if (_client is null)
        {
            Console.WriteLine("procmon: error: Could not write a request. No connection was established.");
            return;
        }
 
        try 
        {
            var requestBytes = _serializer.Serialize(request);

            var lengthBytes = BitConverter.GetBytes(requestBytes.Length);

            await _client.WriteAsync(lengthBytes, 0, 4);
        
            await _client.WriteAsync(requestBytes, 0, requestBytes.Length);

            await _client.FlushAsync();
        }
        catch (Exception)
        {
            Console.WriteLine("procmon: error: could not write a request to the `Commands` pipe.");
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
   
    public async Task<bool> ConnectAsync()
    {
        if (!_backend.Create()) 
        {
            Console.WriteLine($"procmon: error: {_backend.GetErrorString()}.");
            return false; 
        }

        if (!TryCreateClientStream()) return false;

        try 
        { 
            if (_client is null) return false;

            Console.WriteLine("procmon: info: trying to connect to the `Commands` pipe.");
            
            await _client.ConnectAsync();
                
            Console.WriteLine("procmon: info: Connected to the `Commands` pipe.");
        }
        catch (InvalidOperationException) 
        {
            Console.WriteLine("procmon: error: Could not connect to `Commands` pipe: already connected.");
        }            

        return IsConnected;
    }
}
