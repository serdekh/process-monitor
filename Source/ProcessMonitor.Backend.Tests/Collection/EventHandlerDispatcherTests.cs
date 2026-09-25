using System.Threading.Channels;
using Microsoft.Diagnostics.Tracing;
using Moq;
using ProcessMonitor.Backend.Collection;
using ProcessMonitor.Backend.Models;
using ProcessMonitor.Backend.Models.Collection;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Backend.State;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Tests.Collection;

public class EventHandlerDispatcherTests
{
    [Fact]
    public void DispatchEvent_ReturnsFailure_WhenUpdatingProcessIdFails()
    {
        // Arrange
        var expectedProcessId = 42;

        var fakeEvent = new Mock<ITraceEvent>();
        var fakeCtx = new Mock<IEventCollectorContext>();
        var fakeCtxFailure = 
            new Failure<None, CollectionError, CollectionWarning>(
                new ErrorChain<CollectionError>(
                    new ProcessIdUpdateFailed(), null));

        fakeCtx.Setup(e => e.ProcessId).Returns(expectedProcessId);
        fakeCtx.Setup(e => e.TryUpdateTargetProcess()).Returns(fakeCtxFailure);
        
        var dispatcher = new EventHandlerDispatcher(fakeCtx.Object);

        // Act
        var result = dispatcher.DispatchEvent(fakeEvent.Object);

        // Assert
        Assert.True(result is Failure<None, CollectionError, CollectionWarning>);

        var failure = (Failure<None, CollectionError, CollectionWarning>)result;
        Assert.True(failure.Chain.Error is EventDispatchingFailed);
    }

    [Fact]
    public void DispatchEvent_ReturnsSuccess_WhenEventCollectorContextHasNoProcessId()
    {
        // Arrange
        var fakeEvent = new Mock<ITraceEvent>();
        var fakeCtx = new Mock<IEventCollectorContext>();
        var fakeCtxSuccessTryUpdateTargetProcess = new Success<None, CollectionError, CollectionWarning>(new None());

        fakeCtx.Setup(e => e.TryUpdateTargetProcess()).Returns(fakeCtxSuccessTryUpdateTargetProcess);
        fakeCtx.Setup(e => e.HasProcessId).Returns(false);

        var dispatcher = new EventHandlerDispatcher(fakeCtx.Object);

        // Act
        var result = dispatcher.DispatchEvent(fakeEvent.Object);

        // Assert
        Assert.True(result is Success<None, CollectionError, CollectionWarning>);
    }


    [Fact]
    public void DispatchEvent_ReturnsSuccess_WhenHappyPath()
    {
        // Arrange
        var fakeEvent = new Mock<ITraceEvent>();
        var fakeCtx = new Mock<IEventCollectorContext>();
        var fakeCtxSuccessTryUpdateTargetProcess = new Success<None, CollectionError, CollectionWarning>(new None());

        fakeCtx.Setup(e => e.TryUpdateTargetProcess()).Returns(fakeCtxSuccessTryUpdateTargetProcess);
        fakeCtx.Setup(e => e.HasProcessId).Returns(true);
        fakeEvent.Setup(e => e.GetRawEventKind()).Returns(RawEventKind.ContextSwitch);

        var dispatcher = new EventHandlerDispatcher(fakeCtx.Object);

        // Act
        var result = dispatcher.DispatchEvent(fakeEvent.Object);

        // Assert
        Assert.True(result is Success<None, CollectionError, CollectionWarning>);
    }

    [Fact]
    public void HandleEvent_ReturnsSuccess_WhenIsRelevant()
    {
        // Arrange
        var input = Channel.CreateUnbounded<RawEvent>();
        var state = new MonitoringSessionState(42);
        var action = new Action(() => {});

        var fakeEvent = new Mock<ITraceEvent>();
        fakeEvent.Setup(e => e.GetRawEventKind()).Returns(RawEventKind.Undefined);

        var ctx = new EventCollectorContext(input, state);
        var dispatcher = new EventHandlerDispatcher(ctx);

        // Act
        var result = dispatcher.HandleEvent(() => true, action, fakeEvent.Object);

        // Assert
        Assert.True(result is Success<None, CollectionError, CollectionWarning>);
    }

    [Fact]
    public void HandleEvent_ReturnsFailure_WhenTryWriteRawEventFails()
    {
        // Arrange
        var action = new Action(() => {});

        var fakeFailure = 
            new Failure<None, CollectionError, CollectionWarning>(
                new ErrorChain<CollectionError>(
                    new EventWriteFailed(new InvalidOperationException(), RawEventKind.Undefined)));

        var fakeEvent = new Mock<ITraceEvent>();
        fakeEvent.Setup(e => e.GetRawEventKind()).Returns(RawEventKind.Undefined);

        var fakeCtx = new Mock<IEventCollectorContext>();
        fakeCtx.Setup(e => e.TryWriteRawEvent(fakeEvent.Object)).Returns(fakeFailure);

        var dispatcher = new EventHandlerDispatcher(fakeCtx.Object);

        // Act
        var result = dispatcher.HandleEvent(() => true, action, fakeEvent.Object);

        // Assert
        Assert.True(result is Failure<None, CollectionError, CollectionWarning>);

        var failure = (Failure<None, CollectionError, CollectionWarning>)result;

        Assert.True(failure.Chain.Error is EventHandlingError);
    }

    [Fact]
    public void HandleEvent_ReturnsSuccess_WhenIsNotRelevant()
    {
        // Arrange
        var action = new Action(() => {});

        var fakeEvent = new Mock<ITraceEvent>();

        var fakeCtx = new Mock<IEventCollectorContext>();

        var dispatcher = new EventHandlerDispatcher(fakeCtx.Object);

        // Act
        var result = dispatcher.HandleEvent(() => false, action, fakeEvent.Object);

        // Assert
        Assert.True(result is Success<None, CollectionError, CollectionWarning>);
    }
}