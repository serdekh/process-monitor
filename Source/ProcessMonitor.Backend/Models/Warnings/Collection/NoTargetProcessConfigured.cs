namespace ProcessMonitor.Backend.Models.Warnings.Collection;

public record NoTargetProcessConfigured : CollectionWarning
{
    public override string ToString()
    {
        return $"No value set for target process";
    }
}