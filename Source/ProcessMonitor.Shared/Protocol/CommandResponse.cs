namespace ProcessMonitor.Shared.Protocol;

public sealed class CommandResponse
{
    public int StatusCode { get; set; } = 200;

    public string Message { get; set; } = string.Empty;

    public object? Data { get; set; }
}
