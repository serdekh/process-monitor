using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

using ProcessMonitor.Backend.State;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventStreamCollector(
    Channel<TraceEvent> input,
    ILogger<EventStreamCollector> logger,
    IHostApplicationLifetime hostLifetime,
    MonitoringSessionState state) : IEventCollector
{
    // NOTE: Consider adding a configuration manager in the future that handles
    // loading a custom session name
    public static string SessionName { get; } = "ProcessMonitor.Backend.TraceEventSession";

    private readonly ChannelWriter<TraceEvent> _writer = input.Writer;

    private readonly ILogger<EventStreamCollector> _logger = logger;

    private readonly IHostApplicationLifetime _hostLifetime = hostLifetime;

    private readonly MonitoringSessionState _state = state;

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

    private async Task<Exception?> TryWriteEvent(TraceEvent e, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new InvalidOperationException("Could not write event: cancellation requested");

        var processId = _state.ProcessId;

        if (processId is null) return null;

        if (processId != e.ProcessID) return null;

        try
        {
            await _writer.WriteAsync(e.Clone(), ct);
            return null;
        } 
        catch (Exception ex)
        {
            _hostLifetime.StopApplication();
            return ex;
        }
    }

    public async Task RunAsync(CancellationToken ct)
    {
        if (!TryInitializeCollection()) return;

        using var session = new TraceEventSession(SessionName);

        var kernelKeywords = KernelTraceEventParser.Keywords.Process 
            | KernelTraceEventParser.Keywords.Thread 
            | KernelTraceEventParser.Keywords.ContextSwitch
            | KernelTraceEventParser.Keywords.SystemCall;
        
        session.EnableKernelProvider(kernelKeywords);

        session.Source.Kernel.ProcessStart += async data => await TryWriteEvent(data, ct);
        session.Source.Kernel.ProcessStop += async data => await TryWriteEvent(data, ct);

        session.Source.Kernel.ThreadStart += async data => await TryWriteEvent(data, ct);
        session.Source.Kernel.ThreadStop += async data => await TryWriteEvent(data, ct);

        session.Source.Kernel.ImageLoad += async data => await TryWriteEvent(data, ct);
        session.Source.Kernel.ImageUnload += async data => await TryWriteEvent(data, ct);

        session.Source.Kernel.ThreadCSwitch += async data => await TryWriteEvent(data, ct);

        var collecting = Task.Run(() => 
        {
            _logger.LogInformation("[Collection]: Starting...");
            session.Source.Process();
        }, ct);

        try 
        {
            await Task.Delay(Timeout.Infinite, ct);
        }      
        catch (Exception ex)
        {
            _logger.LogError("[Collection]: Could not read input events: {}. Terminating...", ex.Message);
        }

        session.Stop();

        await collecting;

        _logger.LogInformation("[Collection]: Terminated.");
    }
}
