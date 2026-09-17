namespace ProcessMonitor.Backend.Models.Errors.Collection;

public record EventDispatchingFailed() : CollectionError
{
    public override string ToString()
    {
        return $"Failed to dispatch event";
    }
}