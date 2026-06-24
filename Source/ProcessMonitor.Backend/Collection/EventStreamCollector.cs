using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;

using ProcessMonitor.Backend.Models;

namespace ProcessMonitor.Backend.Collection;

public sealed class EventStreamCollector : IEventCollector
{
    public static string SessionName { get; } = "ProcessMonitor.Backend.TraceEventSession";

    private ChannelWriter<RawKernelEvent> _writer;

    public EventStreamCollector(Channel<RawKernelEvent> input)
    { 
        _writer = input.Writer;
    }

    // TODO: Add proper logging
    public async Task RunAsync(CancellationToken ct)
    {
        if (TraceEventSession.IsElevated() != true)
        {
            Console.WriteLine("error: could only run as administrator.");
            return;
        }

        using (var oldSession = new TraceEventSession(SessionName))
        {
            oldSession.Stop();
        }

        using (var session = new TraceEventSession(SessionName))
        {
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
            
            var collecting = Task.Run(() => session.Source.Process(), CancellationToken.None);

            try 
            {
                await Task.Delay(Timeout.Infinite, ct);
            } 
            catch (OperationCanceledException)
            {
                Console.WriteLine("info: cancellation requested. Stopping collection");
            }            

            session.Stop();

            await collecting;

            Console.WriteLine("info: collection stopped");
        }
    }
}
