using System.Threading.Channels;
using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.State;

namespace ProcessMonitor.Backend.Tests.Fixtures.Collection;

public class EventCollectorContextFixture
{
    public IEventCollectorContext Context { get; }

    public EventCollectorContextFixture()
    {
        Context = new EventCollectorContext
        (
            Channel.CreateUnbounded<RawEvent>(), 
            new MonitoringSessionState(42)
        );
    }
}