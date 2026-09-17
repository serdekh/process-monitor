namespace ProcessMonitor.Backend.Models.Warnings.Collection;

public record OperationCanceled() : CollectionWarning
{
    public override string ToString()
    {
        return $"Operation canceled";
    }
}