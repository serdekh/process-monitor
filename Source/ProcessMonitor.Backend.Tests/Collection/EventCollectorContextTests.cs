using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Tests.Fixtures.Collection;
using ProcessMonitor.Backend.Tests.Mockers;
using Moq;
using ProcessMonitor.Backend.Models.Collection;
using System.Threading.Channels;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Backend.State;
using ProcessMonitor.Shared.Models.Results;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Backend.Models.Errors.Collection;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

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

    [Fact]
    public void TryWriteRawEvent_ReturnsSuccess_WhenChannelHasSpace()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<RawEvent>();

        var eventCollectorContext = new EventCollectorContext(channel, new MonitoringSessionState(42));

        var fakeEvent = new Mock<ITraceEvent>();
        var expectedKind = RawEventKind.Undefined;

        fakeEvent.Setup(e => e.GetRawEventKind()).Returns(expectedKind);
        fakeEvent.Setup(e => e.CloneAsRawEvent()).Returns(new RawEvent(null, expectedKind));

        // Act
        var result = eventCollectorContext.TryWriteRawEvent(fakeEvent.Object);

        //Assert
        Assert.True(result is Success<None, CollectionError, CollectionWarning>);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void TryWriteRawEvent_ReturnsWarning_WhenChannelIsFull()
    {
        // Arrange
        var options = new BoundedChannelOptions(1) 
        { 
            FullMode = BoundedChannelFullMode.Wait
        };
        
        var channel = Channel.CreateBounded<RawEvent>(options);
        var writer = channel.Writer;

        var expectedKind = RawEventKind.Undefined;
        writer.TryWrite(new RawEvent(null, expectedKind)); 

        var eventCollectorContext = new EventCollectorContext(channel, new MonitoringSessionState(1));

        var fakeEvent = new Mock<ITraceEvent>();
        fakeEvent.Setup(e => e.GetRawEventKind()).Returns(expectedKind);
        fakeEvent.Setup(e => e.CloneAsRawEvent()).Returns(new RawEvent(null!, expectedKind));

        // Act
        var result = eventCollectorContext.TryWriteRawEvent(fakeEvent.Object);

        // Assert
        Assert.True(result is Success<None, CollectionError, CollectionWarning>);
        Assert.Contains(result.Warnings, w => w is EventWriteRejected);
    }

    [Fact]
    public void TryWriteRawEvent_ReturnsFailure_WhenCloningFails()
    {
        // Arrange
        var channel = Channel.CreateUnbounded<RawEvent>();

        var eventCollectorContext = new EventCollectorContext(channel, new MonitoringSessionState(42));

        var fakeEvent = new Mock<ITraceEvent>();
        var expectedException = new InsufficientMemoryException();

        fakeEvent.Setup(e => e.CloneAsRawEvent()).Throws(expectedException);

        // Act
        var result = eventCollectorContext.TryWriteRawEvent(fakeEvent.Object);

        // Assert
        Assert.True(result is Failure<None, CollectionError, CollectionWarning>);

        var failure = (Failure<None, CollectionError, CollectionWarning>)result;
        var actualError = failure.Chain.Error as EventWriteFailed;

        Assert.NotNull(actualError);
        Assert.Same(expectedException, actualError.Exception);
    }

    // TODO: ADD tests for TryUpdateTargetProcess
    // TODO: ADD tests for TrySeedExistingThreads
}