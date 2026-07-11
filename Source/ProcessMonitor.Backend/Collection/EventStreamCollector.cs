using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

using Microsoft.Diagnostics.Tracing;
using System;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventStreamCollector(
    Channel<TraceEvent> input,
    ILogger<EventStreamCollector> logger,
    IHostApplicationLifetime hostLifetime) : IEventCollector
{
    // NOTE: Consider adding a configuration manager in the future that handles
    // loading a custom session name
    public static string SessionName { get; } = "ProcessMonitor.Backend.TraceEventSession";

    private readonly ChannelWriter<TraceEvent> _writer = input.Writer;

    private readonly ILogger<EventStreamCollector> _logger = logger;

    private readonly IHostApplicationLifetime _hostLifetime = hostLifetime;

    private bool TryInitializeCollection()
    {
        using var oldSession = new TraceEventSession(SessionName);

        _logger.LogDebug("[Collection]: (Init) Stopping previously created {sessionName} session", SessionName);

        oldSession.Stop();

        if (TraceEventSession.IsElevated() != true)
        {
            _logger.LogError("[Collection]: Could only run as administrator.");
            _hostLifetime.StopApplication();
            return false;
        }

        return true;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (!TryInitializeCollection()) return;

        using var session = new TraceEventSession(SessionName);
        
        session.EnableKernelProvider(KernelTraceEventParser.Keywords.Process);

        session.Source.Kernel.ProcessStart += data => _writer.TryWrite(data.Clone());
        session.Source.Kernel.ProcessStop += data => _writer.TryWrite(data.Clone());

        session.Source.Kernel.ThreadStart += data => _writer.TryWrite(data.Clone());
        session.Source.Kernel.ThreadStop += data => _writer.TryWrite(data.Clone());

        session.Source.Kernel.ImageLoad += data => _writer.TryWrite(data.Clone());
        session.Source.Kernel.ImageUnload += data => _writer.TryWrite(data.Clone());

        var collecting = Task.Run(() => 
        {
            _logger.LogInformation("[Collection]: Starting...");
            session.Source.Process();
        }, ct);

        try 
        {
            await Task.Delay(Timeout.Infinite, ct);
        } 
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[Collection]: Cancellation requested. Terminating...");
        }            
        catch (Exception ex)
        {
            _logger.LogInformation("[Collection]: Could not read input events: {}. Terminating...", ex.Message);
        }

        session.Stop();

        await collecting;

        _logger.LogInformation("[Collection]: Terminated.");
    }
}
