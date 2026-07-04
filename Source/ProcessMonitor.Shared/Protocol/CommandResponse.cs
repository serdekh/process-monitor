namespace ProcessMonitor.Shared.Protocol;

public sealed class CommandResponse
{
    public int StatusCode { get; init; } = 200;

    public string Message { get; init; } = string.Empty;

    public object? Data { get; init; }
}
