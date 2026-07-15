using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ProcessMonitor.CLI.Common;

using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Transport;

namespace ProcessMonitor.CLI.Transport;

public sealed class CommandPipeClient : IAsyncDisposable
{
    private NamedPipeClientStream? _client = null;

    private IPCProtocolWriter? _writer = null;

    private readonly IMessageSerializer _serializer;

    private readonly BackendProcess _backend;

    private readonly ILogger<CommandPipeClient> _logger;

    public bool IsConnected => _backend.IsRunning && (_client?.IsConnected ?? false);

    public CommandPipeClient(
        BackendProcess backend, 
        IMessageSerializer serializer, 
        ILogger<CommandPipeClient> logger)
    {
        _backend = backend;  
        _logger = logger;
        _serializer = serializer;
    }
    
    public async Task CleanupConnection(bool killBackend = false)
    {
        _logger.LogInformation("[Commands]: Attempting to disconnect.");

        if (killBackend) await _backend.DisposeAsync();
    
        if (_client is not null) 
        {
            _client.Close();

            _client.Dispose();

            _client = null;   
        }

        _logger.LogInformation("[Commands]: Disconnection complete.");
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupConnection();
    }

    // TODO: Implement server response handling
    public async Task<bool> WriteAsync(MessageEnvelope<CommandRequest> request, CancellationToken ct)
    {
        if (_client is null || _writer is null)
        {
            _logger.LogError("[Commands]: Could not write a request. No connection was established.");
            return false;
        }

        if (ct.IsCancellationRequested)
        {
            _logger.LogError("[Commands]: Could not write a request: cancellation requested.");
            return false;
        }
 
        try 
        {
            var requestBytes = _serializer.Serialize(request, prefixed: true);

            if (requestBytes is null)
            {
                _logger.LogError("[Commands]: Could not serialize a request: {}.", _serializer.GetError()?.Message ?? "unknown error");
                return false;
            }

            try
            {
                await _writer.WriteAsync(requestBytes, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError("[Commands]: Could not send a request: {}", ex.Message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("[Commands]: Could not write a request to the 'Commands' pipe: {}", ex.Message);
            return false;
        }
    }
 
    private bool TryCreateClientStream()
    {
        if (_client is not null) return true;

        try
        {
            _client = new NamedPipeClientStream(
                ".", 
                "ProcessMonitor.Pipes.Commands", 
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            _writer = new IPCProtocolWriter(_client);
        }
        catch (Exception ex)
        {
            _logger.LogError("[Commands]: Could not create the 'Commands' pipe client: {}", ex.Message);
            return false;
        }

        _logger.LogInformation("[Commands]: Created the 'Commands' pipe client.");
        return true;
    }
   
    public async Task<bool> TryConnectAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogError("[Commands]: Could not connect to a server: cancellation requested.");
            await CleanupConnection();
            return false;
        }

        if (!_backend.Create()) 
        {
            _logger.LogError("[Commands]: {}.", _backend.GetErrorString());
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

            _logger.LogInformation("[Commands]: Attempting to connect to the 'Commands' pipe.");
            
            await _client.ConnectAsync(ct);
                
            _logger.LogInformation("[Commands]: Successfully connected to the 'Commands' pipe.");
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogError("[Commands]: Could not connect to the 'Commands' pipe: access denied.");
            await CleanupConnection();
        }
        catch (InvalidOperationException) 
        {
            _logger.LogError("[Commands]: Could not connect to 'Commands' pipe: already connected.");
        }            

        return IsConnected;
    }
}
