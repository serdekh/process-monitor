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

        _pipe = new NamedPipeServerStream
        (
            "ProcessMonitor.Pipes.Telemetry",
            PipeDirection.Out,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous
        );
    }

    public void CleanupConnection()
    {
        _pipe?.Dispose(); _pipe = null;
        _logger.LogInformation("Transport: the pipe has been closed.");
    }

    public void Dispose()
    {
        CleanupConnection();
    }
 
    public async Task SendAsync(byte[] data, CancellationToken ct)
    {
        if (_pipe is null)
        {
            _logger.LogWarning("Transport: cannot send bytes, no `Telemetry` pipe has been created.");
            return;
        }

        await _pipe.WaitForConnectionAsync();

        _logger.LogDebug("Transport: connected with a client.");

        try 
        {
//            await using var writer = new StreamWriter(_pipe) { AutoFlush = true };

            // TODO: Add length prefix for the future protocol
            await _pipe.WriteAsync(data, ct);
        }
        catch (Exception) 
        {
            _logger.LogError("Transport: failed to send metrics via the `Telemetry` pipe.");
        }
    }
}
