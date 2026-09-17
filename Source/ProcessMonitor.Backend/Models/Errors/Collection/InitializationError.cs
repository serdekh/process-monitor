namespace ProcessMonitor.Backend.Models.Errors.Collection;

public record InitializationError() : CollectionError
{
    public override string ToString()
    {
        return $"Failed to initialize event listening session";
    }
}