using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Tests.Fixtures.Collection;
using ProcessMonitor.Backend.Tests.Mockers;
using Moq;
using ProcessMonitor.Backend.Models.Collection;

namespace ProcessMonitor.Backend.Tests.Collection;

public class EventCollectorContextTests(EventCollectorContextFixture fixture) 
    : IClassFixture<EventCollectorContextFixture>
{
    private readonly IEventCollectorContext _ctx = fixture.Context;

    [Fact]
    public void HasProcessId_ReturnsFalse_WhenProcessIdIsNull()
    {
        // Arrange
        _ctx.ProcessId = null;

        // Act
        var actual = _ctx.HasProcessId;

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void HasProcessId_ReturnsTrue_WhenProcessIdIsNotNull()
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

    [Fact]
    public void IsEventRelevantToProcessId_ReturnsFalse_WhenProcessIdIsNull()
    {
        // Arrange
        _ctx.ProcessId = null;
        var fakeEvent = new Mock<ITraceEvent>();

        // Act
        var actual = _ctx.IsEventRelevantToProcessId(fakeEvent.Object);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void IsEventRelevantToProcessId_ReturnsTrue_WhenProcessIdsAreEqual()
    {
        // Arrange
        var random = new Random();
        var randomId = random.Next(1, 1000);

        _ctx.ProcessId = randomId;
        var fakeEvent = new Mock<ITraceEvent>();

        fakeEvent.Setup(e => e.ProcessId).Returns(randomId);

        // Act
        var actual = _ctx.IsEventRelevantToProcessId(fakeEvent.Object);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void IsEventRelevantToProcessId_ReturnsFalse_WhenProcessIdsAreNotEqual()
    {
        // Arrange
        var random = new Random();
        var randomId = random.Next(1, 1000);

        _ctx.ProcessId = randomId;
        var fakeEvent = new Mock<ITraceEvent>();

        fakeEvent.Setup(e => e.ProcessId).Returns(randomId + 1);

        // Act
        var actual = _ctx.IsEventRelevantToProcessId(fakeEvent.Object);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void IsContextSwitchRelevantToProcessId_ReturnsFalse_WhenProcessIdIsNull()
    {
        // Arrange
        _ctx.ProcessId = null;
        var fakeEvent = new Mock<IContextSwitchEvent>();

        // Act
        var actual = _ctx.IsContextSwitchRelevantToProcessId(fakeEvent.Object);

        // Assert
        Assert.False(actual);
    }

    // TODO: Add tests for IsContextSwitchRelevantToProcessId
    // TODO: Add tests for TryWriteRawEvent
    // TODO: ADD tests for TryUpdateTargetProcess
    // TODO: ADD tests for TrySeedExistingThreads
}