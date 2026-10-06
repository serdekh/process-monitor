using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Models.Collection;
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

        return Result.Success<None, CollectionError, CollectionWarning>(None.New());
    }

    private Result<None, CollectionError, CollectionWarning> IsElevated() =>
        TraceEventSession.IsElevated() == true
            ? Result.Success<None, CollectionError, CollectionWarning>(None.New())
            : Result.Failure<None, CollectionError, CollectionWarning>(new CouldOnlyRunAsAdministrator());

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
            var dispatchingResult = _dispatcher.DispatchEvent(new TraceEventWrapper(data));

            if (dispatchingResult.IsFailure())
            {
                _initializationFailed = Result.Failure<None, CollectionError, CollectionWarning>(new InitializationError(), dispatchingResult.AsFailure().Chain);
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
            catch (OperationCanceledException) { }
            finally
            {
                session.Stop();
                await processingTask;
                _ctx.TryCompleteWriting();
            }

            return _initializationFailed is not null
                ? _initializationFailed
                : Result.Success<None, CollectionError, CollectionWarning>(None.New());
        }
    }

    public async Task<Result<None, CollectionError, CollectionWarning>> RunAsync(CancellationToken ct)
    {
        StopOldSession();

        var isElevated = IsElevated();

        if (isElevated.IsFailure()) return isElevated.AsFailure();

        var session = InitializeSession();

        return await ProcessEventsAsync(session, ct);
    }
}