using System.Text.Json;

namespace ProcessMonitor.Shared.Protocol;

public sealed class CommandRequest
{
    public string Method { get; init; } = string.Empty;

    public string Route { get; init; } = string.Empty;

    public JsonElement? Body { get; init; }
}
