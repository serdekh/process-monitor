namespace ProcessMonitor.Backend.Models.Errors.Collection;

public record ProcessIdUpdateFailed() : CollectionError
{
    public override string ToString()
    {
        return $"Failed to update target process id";
    }
}