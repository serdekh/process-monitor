using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

namespace ProcessMonitor.Shared.Transport;

public class TelemetryTransport : ITelemetryTransport, IDisposable
{
    private ILogger<TelemetryTransport> _logger;

    private NamedPipeServerStream? _pipe = null;

    public TelemetryTransport(ILogger<TelemetryTransport> logger)
    {
        _logger = logger;
    }

    public void Dispose()
    {
        Deinitialize();
    }
 
    public async Task SendAsync(byte[] data, CancellationToken ct)
    {
        if (_pipe is null)
        {
            _logger.LogError("[Ttransport]: Cannot send telemetry metrics: no connection is established.");
            return;
        }

        _logger.LogDebug("[Transport]: Successfully connected with a client.");

        try 
        {
            await _pipe.WriteAsync(data, ct);
        }
        catch (Exception ex) 
        {
            _logger.LogError("[Transport]: Failed to send metrics via the 'Telemetry' pipe: {}", ex.Message);
            Deinitialize();
        }
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogInformation("[Transport]: Could not initialize a telemetry pipe server stream: cancellation requested.");
            Deinitialize();
            return;
        }

        if (_pipe is not null)
        {
            _logger.LogDebug("[Transport]: Attempting to initialize an already created telemetry pipe server stream. Ignore.");
            return;
        }

        _pipe = new NamedPipeServerStream
        (
            "ProcessMonitor.Pipes.Telemetry",
            PipeDirection.Out,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous
        );

        await _pipe.WaitForConnectionAsync(ct);
    }

    public void Deinitialize()
    {
        _pipe?.Close();
        _pipe?.Dispose(); 
        _pipe = null;
        _logger.LogInformation("[Transport]: The telemetry server pipe stream has been finalized.");
    }
}
