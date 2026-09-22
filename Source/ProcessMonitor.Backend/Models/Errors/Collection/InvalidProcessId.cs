namespace ProcessMonitor.Backend.Models.Errors.Collection;

public record InvalidProcessId(int ProcessId) : CollectionError
{
    public override string ToString()
    {
        return $"{ProcessId} is not valid process id";
    }
}