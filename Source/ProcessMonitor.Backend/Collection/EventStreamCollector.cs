using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Backend.State;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventStreamCollector : IEventCollector
{
    public static string SessionName => "ProcessMonitor.Backend.TraceEventSession";

    private readonly ILogger<EventStreamCollector> _logger;
    private readonly EventCollectorContext _ctx;
    private readonly EventHandlerDispatcher _dispatcher;

    private Failure<None, CollectionError, CollectionWarning>? _initializationFailed = null;

    public EventStreamCollector(
        Channel<RawEvent> input,
        ILogger<EventStreamCollector> logger,
        MonitoringSessionState state)
    {
        _ctx = new EventCollectorContext(input, state);
        _dispatcher = new EventHandlerDispatcher(_ctx);

        _logger = logger;
    }

    private Success<None, CollectionError, CollectionWarning> StopOldSession()
    {
        using var oldSession = new TraceEventSession(SessionName);

        _logger.LogDebug("[Collection]: Stopping previously created {SessionName} session.", SessionName);

        oldSession.Stop();
        
        return new Success<None, CollectionError, CollectionWarning>(new None());
    }

    private Result<None, CollectionError, CollectionWarning> IsElevated()
    {
        if (TraceEventSession.IsElevated() == true)
        {
            return new Success<None, CollectionError, CollectionWarning>(new None());
        }

        return new Failure<None, CollectionError, CollectionWarning>(
            new ErrorChain<CollectionError>(
                new CouldOnlyRunAsAdministrator()));
    }

    private TraceEventSession InitializeSession()
    {
        var session = new TraceEventSession(SessionName);

        var kernelKeywords =
            KernelTraceEventParser.Keywords.Process
            | KernelTraceEventParser.Keywords.Thread
            | KernelTraceEventParser.Keywords.ContextSwitch
            | KernelTraceEventParser.Keywords.SystemCall;

        session.EnableKernelProvider(kernelKeywords);

        session.Source.Kernel.All += (data) =>
        {
            var dispatchingResult = _dispatcher.DispatchEvent(data);

            if (dispatchingResult is Failure<None, CollectionError, CollectionWarning> failure)
            {
                _initializationFailed = new Failure<None, CollectionError, CollectionWarning>(
                    new ErrorChain<CollectionError>(
                        new InitializationError(), failure.Chain));
                
                session.Stop(); 
            }
        };

        return session;
    }

    private async Task<Result<None, CollectionError, CollectionWarning>> ProcessEventsAsync(TraceEventSession session, CancellationToken ct)
    {
        using (session) 
        {
            var processingTask = Task.Run(session.Source.Process, CancellationToken.None);

            try
            {
                while (!ct.IsCancellationRequested && _initializationFailed is null)
                {
                    await Task.Delay(100, ct);
                }
            }
            catch (OperationCanceledException) {}
            finally
            {
                session.Stop();
                await processingTask;
                _ctx.TryCompleteWriting();
            }

            if (_initializationFailed is not null)
            {
                return _initializationFailed;
            }

            return new Success<None, CollectionError, CollectionWarning>(new None());
        }
    }

    public async Task<Result<None, CollectionError, CollectionWarning>> RunAsync(CancellationToken ct)
    {
        StopOldSession();

        if (IsElevated() is Failure<None, CollectionError, CollectionWarning> failure) return failure;
        
        var session = InitializeSession();
        
        return await ProcessEventsAsync(session, ct);
    }
}