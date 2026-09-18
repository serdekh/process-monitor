using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Tests.Mockers.Collection;

namespace ProcessMonitor.Backend.Tests.Fixtures.Collection;

public class EventCollectorContextFixture
{
    public IEventCollectorContext Context { get; }

    public EventCollectorContextFixture()
    {
        Context = new EventCollectorContextMock();
    }
}