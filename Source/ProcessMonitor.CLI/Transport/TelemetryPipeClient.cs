using System;
using System.IO;
using System.Text;
using System.IO.Pipes;
using System.Threading;
using System.Diagnostics;
using System.Buffers.Binary;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using ProcessMonitor.CLI.Common;

using ProcessMonitor.Shared.Protocol;
using ProcessMonitor.Shared.Snapshots;
using ProcessMonitor.Shared.Transport;
using ProcessMonitor.Shared.Serialization;

namespace ProcessMonitor.CLI.Transport;

public sealed class TelemetryPipeClient : IAsyncDisposable
{
    private BackendProcess _backend;

    private NamedPipeClientStream? _telemetryClient;

    private ILogger<TelemetryPipeClient> _logger;

    private readonly IMessageSerializer _serializer;

    private IPCProtocolReader? _reader = null;

    public bool IsConnected => _backend.IsRunning && (_telemetryClient?.IsConnected ?? false);

    public TelemetryPipeClient(
        BackendProcess backend, 
        ILogger<TelemetryPipeClient> logger,
        IMessageSerializer serializer)
    {
        _backend = backend;
        _logger = logger;
        _serializer = serializer;

        _backend.AddOnExitHandler(async (sender, e) =>
        {
            _logger.LogInformation("[Telemetry]: The backend process has exited. Run the 'create' command to reconnect.");
            await CleanupConnectionAsync();
        });
    }

    public async Task CleanupConnectionAsync()
    {
        _logger.LogInformation("[Telemetry]: Attempting to disconnect.");

        await _backend.KillAsync();

        if (_telemetryClient is null) return;
        
        _telemetryClient.Close();

        _telemetryClient.Dispose();

        _telemetryClient = null;   

        _logger.LogInformation("[Telemetry]: Disconnection complete.");
    }

    private bool TryCreateClientStream()
    {
        if (_telemetryClient is not null)
        {
            _logger.LogInformation("[Telemetry]: The client stream has already been created. Ignore.");    
            return true;
        }

        try
        {    
            _telemetryClient = new NamedPipeClientStream(
                ".", 
                "ProcessMonitor.Pipes.Telemetry", 
                PipeDirection.In,
                PipeOptions.Asynchronous);

            _reader = new IPCProtocolReader(_telemetryClient);
        }
        catch (Exception ex)
        {
            _logger.LogError("[Telemetry]: Could not create a client stream: {}.", ex);
            return false;
        }

        _logger.LogInformation("[Telemetry]: Successfully created a client stream.");
        return true;
    }

    public async Task<bool> TryConnectAsync(CancellationToken ct)
    {
        if (IsConnected) return true;

        if (ct.IsCancellationRequested)
        {
            _logger.LogInformation("[Telemetry]: Could not connect to the telemetry pipe: cancellation requested.");    
            await CleanupConnectionAsync();
            return false;
        }

        if (!_backend.Create())
        {
            _logger.LogError("[Telemetry]: Could not create a backend process: {}.", _backend.GetErrorString());
            await CleanupConnectionAsync();
            return false;
        }

        if (!TryCreateClientStream())
        {
            await CleanupConnectionAsync();
            return false;
        }

        try
        {
            _logger.LogInformation("[Telemetry]: Attempting to connect to the 'Telemetry' pipe...");

            Debug.Assert(_telemetryClient is not null, "The telemetry client is expected to be handled at the 'TryCreateClientStream' method.");

            await _telemetryClient.ConnectAsync(ct);

            _logger.LogInformation("[Telemetry]: Successfully connected to the 'Telemetry' pipe.");
        }
        catch (Exception ex)
        {
            _logger.LogError("[Telemetry]: Failed to connect to the 'Telemetry' pipe: {}.", ex.Message);
        }

        return true;
    }

    public async Task<bool> ReadAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogError("[Telemetry]: Could not start reading a telemetry message: cancellation requested.");    
            return false;
        }

        if (!IsConnected)
        {
            _logger.LogError("[Telemetry]: Failed to read a telemetry message: no connection.");
            return false;    
        }

        Debug.Assert(_telemetryClient is not null, "_telemetryClient is expected to be non-null in here. If you see this message, then there is a bug");

        Debug.Assert(_reader is not null, "_reader is expected to be non-null in here. If you see this message, then there is a bug");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var bytes = await _reader.ReadAsync(ct);

                if (bytes is null)
                {
                    _logger.LogError("[Telemetry]: Failed to read a telemetry message: Could not read from the pipe.");
                    return false;
                }

                var snapshot = _serializer.Deserialize<MessageEnvelope<ProcessMetricsSnapshot>>(bytes, prefixed: false);

                if (snapshot is null)
                {
                    _logger.LogError("[Telemetry]: Failed to read a telemetry message: Could not deserialize the input snapshot.");
                    return false;
                }

                System.Console.WriteLine(snapshot.Payload.ToString());
            }   
            catch (Exception ex)
            {
                _logger.LogError("[Telemetry]: Failed to read a telemetry message: {}", ex.Message);
                return false;
            }
        }

        return true;
    }

    public async ValueTask DisposeAsync()
    {
        await CleanupConnectionAsync();
    }
}