using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Tests.Fixtures.Collection;

namespace ProcessMonitor.Backend.Tests.Collection;

public class EventCollectorContextTests(EventCollectorContextFixture fixture) 
    : IClassFixture<EventCollectorContextFixture>
{
    private readonly IEventCollectorContext _ctx = fixture.Context;

    [Fact]
    public void HasProcessId_ReturnsFalseWhenProcessIdIsNull()
    {
        // Arrange
        _ctx.ProcessId = null;

        // Act
        var actual = _ctx.HasProcessId;

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void HasProcessId_ReturnsTrueWhenProcessIdIsNotNull()
    {
        // Arrange
        var random = new Random();
        var randomId = random.Next(0, 10000);
        _ctx.ProcessId = randomId;

        // Act
        var actual = _ctx.HasProcessId;

        // Assert
        Assert.True(actual);
    }

    // TODO: Add tests for IsEventRelevantToProcessId
    // TODO: Add tests for IsContextSwitchRelevantToProcessId
    // TODO: Add tests for TryWriteRawEvent
    // TODO: ADD tests for TryUpdateTargetProcess
    // TODO: ADD tests for TrySeedExistingThreads
}