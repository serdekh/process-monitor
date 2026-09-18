using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Tests.Fixtures.Collection;
using ProcessMonitor.Backend.Tests.Mockers;
using Moq;
using ProcessMonitor.Backend.Models.Collection;

namespace ProcessMonitor.Backend.Tests.Collection;

public class EventCollectorContextTests(EventCollectorContextFixture fixture) 
    : IClassFixture<EventCollectorContextFixture>
{
    private readonly Random _random = new();
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
        var randomId = _random.Next(0, 10000);
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
        var randomId = _random.Next(1, 1000);

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
        var randomId = _random.Next(1, 1000);

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

    [Fact]
    public void IsContextSwitchRelevantToProcessId_ReturnsTrue_WhenOldThreadIdIsInHashSet()
    {
        // Arrange
        var randomOldThreadId = _random.Next(1, 1000);

        _ctx.ProcessId = 42;
        _ctx.ProcessThreadIds = [randomOldThreadId];

        var fakeEvent = new Mock<IContextSwitchEvent>();

        fakeEvent.Setup(e => e.OldThreadID).Returns(randomOldThreadId);

        // Act
        var actual = _ctx.IsContextSwitchRelevantToProcessId(fakeEvent.Object);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void IsContextSwitchRelevantToProcessId_ReturnsTrue_WhenNewThreadIdIsInHashSet()
    {
        // Arrange
        var randomNewThreadId = _random.Next(1, 1000);

        _ctx.ProcessId = 42;
        _ctx.ProcessThreadIds = [randomNewThreadId];

        var fakeEvent = new Mock<IContextSwitchEvent>();

        fakeEvent.Setup(e => e.NewThreadID).Returns(randomNewThreadId);

        // Act
        var actual = _ctx.IsContextSwitchRelevantToProcessId(fakeEvent.Object);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void IsContextSwitchRelevantToProcessId_ReturnsTrue_WhenEitherThreadIdIsInHashSet()
    {
        // Arrange
        var randomOldThreadId = _random.Next(1, 1000);
        var randomNewThreadId = _random.Next(1, 1000);

        _ctx.ProcessId = 42;
        _ctx.ProcessThreadIds = [randomOldThreadId, randomNewThreadId];

        var fakeEvent = new Mock<IContextSwitchEvent>();

        fakeEvent.Setup(e => e.OldThreadID).Returns(randomOldThreadId);
        fakeEvent.Setup(e => e.NewThreadID).Returns(randomNewThreadId);

        // Act
        var actual = _ctx.IsContextSwitchRelevantToProcessId(fakeEvent.Object);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void IsContextSwitchRelevantToProcessId_ReturnsFalse_WhenNeitherThreadIdIsInHashSet()
    {
        // Arrange
        var randomOldThreadId = _random.Next(1, 1000);
        var randomNewThreadId = _random.Next(1, 1000);

        _ctx.ProcessId = 42;
        _ctx.ProcessThreadIds = [randomOldThreadId + 1, randomNewThreadId + 1];

        var fakeEvent = new Mock<IContextSwitchEvent>();

        fakeEvent.Setup(e => e.OldThreadID).Returns(randomOldThreadId);
        fakeEvent.Setup(e => e.NewThreadID).Returns(randomNewThreadId);

        // Act
        var actual = _ctx.IsContextSwitchRelevantToProcessId(fakeEvent.Object);

        // Assert
        Assert.False(actual);
    }

    // TODO: Add tests for TryWriteRawEvent
    // TODO: ADD tests for TryUpdateTargetProcess
    // TODO: ADD tests for TrySeedExistingThreads
}