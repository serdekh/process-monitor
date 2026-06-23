using System.Text.Json;

namespace ProcessMonitor.Contracts.Protocol;

public sealed class CommandRequest
{
    public string Methid { get; init; } = string.Empty;

    public string Route { get; init; } = string.Empty;

    public JsonElement? Body { get; init; }
}
