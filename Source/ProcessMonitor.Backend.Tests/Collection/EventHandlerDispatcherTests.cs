using System.Threading.Channels;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;
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
    // TODO: Add tests for 
        //DispatchEvent;
        //HandleEvent
        //HandleThreadStart(TraceEvent data);
        //HandleThreadDCStart(TraceEvent data);
        //HandleThreadStop(TraceEvent data);
        //HandleThreadDCEnd(TraceEvent data);
        //HandleContextSwitch(TraceEvent data);
        //HandleSyscallEnter(TraceEvent data);    
        //HandleUndefined(TraceEvent data);

    // private Channel<RawEvent> _input;
    // private MonitoringSessionState _state;
    // private EventCollectorContext _ctx;

    // public EventHandlerDispatcherTests()
    // {
    //     _input = Channel.CreateUnbounded<RawEvent>();
    //     _state = new MonitoringSessionState(42);
    //     _ctx = new EventCollectorContext(_input, _state);
    // }

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
}