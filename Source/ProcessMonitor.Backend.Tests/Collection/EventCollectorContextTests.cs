using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
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

    [Fact]
    public void IsEventRelevantToProcessId_ReturnsFalseWhenProcessIdIsNull()
    {
        // Arrange
        _ctx.ProcessId = null;
        var e = new TraceEventMock();

        // Act
        var actual = _ctx.IsEventRelevantToProcessId(e);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void IsEventRelevantToProcessId_ReturnsTrueWhenProcessIdsAreEqual()
    {
        // Arrange
        var random = new Random();
        var randomId = random.Next(1, 1000);

        _ctx.ProcessId = randomId;
        var e = new TraceEventMock(randomId);

        // Act
        var actual = _ctx.IsEventRelevantToProcessId(e);

        // Assert
        Assert.True(actual);
    }

    [Fact]
    public void IsEventRelevantToProcessId_ReturnsFalseWhenProcessIdsAreNotEqual()
    {
        // Arrange
        var random = new Random();
        var randomId = random.Next(1, 1000);

        _ctx.ProcessId = randomId + 1;
        var e = new TraceEventMock(randomId);

        // Act
        var actual = _ctx.IsEventRelevantToProcessId(e);

        // Assert
        Assert.False(actual);
    }

    [Fact]
    public void IsContextSwitchRelevantToProcessId_ReturnsFalseWhenProcessIdIsNull()
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