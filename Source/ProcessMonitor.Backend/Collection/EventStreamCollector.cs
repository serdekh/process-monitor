using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

using ProcessMonitor.Backend.Models;
using Microsoft.Extensions.Hosting;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventStreamCollector : IEventCollector
{
    public static string SessionName { get; } = "ProcessMonitor.Backend.TraceEventSession";

    private ChannelWriter<RawKernelEvent> _writer;

    private readonly ILogger<EventStreamCollector> _logger;

    private readonly IHostApplicationLifetime _hostLifetime;

    public EventStreamCollector(
        Channel<RawKernelEvent> input, 
        ILogger<EventStreamCollector> logger,
        IHostApplicationLifetime hostLifetime)
    { 
        _writer = input.Writer;
        _logger = logger;
        _hostLifetime = hostLifetime;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (TraceEventSession.IsElevated() != true)
        {
            _logger.LogError("Collection: Could only run as administrator.");
            
            _hostLifetime.StopApplication();
            return;
        }

        using (var oldSession = new TraceEventSession(SessionName))
        {
            _logger.LogDebug("Collection: Stopping previously created {sessionName} session", SessionName);
            oldSession.Stop();
        }

        using var session = new TraceEventSession(SessionName);
        
        session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);

        session.Source.Kernel.ProcessStart += data => 
        {
            var rawKernelEvent = new RawKernelEvent
            {
                ProcessId = data.ProcessID,
                EventName = data.EventName
            };

            _writer.TryWrite(rawKernelEvent);
        };

        session.Source.Kernel.ProcessStop += data => 
        {
            var rawKernelEvent = new RawKernelEvent
            {
                ProcessId = data.ProcessID,
                EventName = data.EventName
            };

            _writer.TryWrite(rawKernelEvent);
        };
        
        var collecting = Task.Run(() => 
        {
            _logger.LogInformation("Collection: Start event listening.");
            session.Source.Process();
        });

        try 
        {
            await Task.Delay(Timeout.Infinite, ct);
        } 
        catch (OperationCanceledException ex)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogError(ex, "Collection: Cancellation requested.");
            }
            else
            {
                _logger.LogInformation("Collection: Cancellation requested.");
            }
        }            

        session.Stop();

        await collecting;

        _logger.LogInformation("Collection: Stop event listening.");
    }
}
