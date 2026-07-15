using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.State;

using ProcessMonitor.Shared.Protocol;

namespace ProcessMonitor.Backend.Commands.Handlers;

public sealed class StopMonitoringHandler : ICommandHandler
{
    private MonitoringSessionState _state;

    public StopMonitoringHandler(MonitoringSessionState state)
    {
        _state = state;
    }

    public Task<MessageEnvelope<CommandResponse>> HandleAsync(MessageEnvelope<CommandRequest> request, CancellationToken ct)
    {
        var envelope = new MessageEnvelope<CommandResponse>
        {
            Type = MessageType.CommandResponse,
            Payload = new CommandResponse
            {
                StatusCode = 200,
                Message = string.Empty,
                Data = null
            }
        };

        var requestBody = request.Payload.Body;

        if (requestBody is null)
        {
            envelope.Payload.StatusCode = 415;
            
            envelope.Payload.Message = "No body with the process id was provided";

            return Task.FromResult(envelope);
        }

        _state.ResetProcessId();

        envelope.Payload.Message = "The process id was reset successfully";
        System.Console.WriteLine("pid is gone :(");

        return Task.FromResult(envelope);
    }
}