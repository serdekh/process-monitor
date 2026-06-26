using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

using ProcessMonitor.Backend.Models;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventStreamCollector : IEventCollector
{
    public static string SessionName { get; } = "ProcessMonitor.Backend.TraceEventSession";

    private ChannelWriter<RawKernelEvent> _writer;

    private readonly ILogger<EventStreamCollector> _logger;

    public EventStreamCollector(Channel<RawKernelEvent> input, ILogger<EventStreamCollector> logger)
    { 
        _writer = input.Writer;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (TraceEventSession.IsElevated() != true)
        {
            _logger.LogError("Could only run as administrator.");
            return;
        }

        using (var oldSession = new TraceEventSession(SessionName))
        {
            _logger.LogDebug("Stopping previously created {sessionName} session", SessionName);
            oldSession.Stop();
        }

        _logger.LogInformation("Collection: Initializing...");

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
        
        _logger.LogInformation("Collection: Running.");

        var collecting = Task.Run(() => session.Source.Process(), CancellationToken.None);

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

        _logger.LogInformation("Collection: finalizing...");
    }
}
