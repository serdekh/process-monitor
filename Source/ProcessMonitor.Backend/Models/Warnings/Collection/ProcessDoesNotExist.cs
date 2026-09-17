namespace ProcessMonitor.Backend.Models.Warnings.Collection;

public record ProcessDoesNotExist(int ProcessId) : CollectionWarning
{
    public override string ToString()
    {
        return $"Process {ProcessId} does not exist";
    }
}