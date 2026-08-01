namespace ProcessMonitor.Shared.Client.State;
public sealed class ClientApplicationConfiguration
{
    public int? ProcessId { get; set; } = null;

    public string? ServerFilepath { get; set; } = null;
}
