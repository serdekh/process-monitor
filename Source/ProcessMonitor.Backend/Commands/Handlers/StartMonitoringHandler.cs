using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.State;

using ProcessMonitor.Shared.Protocol;

namespace ProcessMonitor.Backend.Commands.Handlers;

public sealed class StartMonitoringHandler : ICommandHandler
{
    private MonitoringSessionState _state;

    public StartMonitoringHandler(MonitoringSessionState state)
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

        if (requestBody?.TryGetProperty("pid", out JsonElement pidElement) is null)
        {
            envelope.Payload.StatusCode = 415;

            envelope.Payload.Message = "The body is missing the 'pid' property name";

            return Task.FromResult(envelope);
        }

        if (!pidElement.TryGetInt32(out int pid))
        {
            envelope.Payload.StatusCode = 400;

            envelope.Payload.Message = "The value of the 'pid' property is not a 32-bit signed integer";

            return Task.FromResult(envelope);
        }

        _state.SetProcessId(pid);

        envelope.Payload.Message = "The process id was updated successfully";
        envelope.Payload.Data = new int[1] { pid };

        return Task.FromResult(envelope);
    }
}