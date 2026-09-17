namespace ProcessMonitor.Backend.Models.Errors.Collection;

public record CouldOnlyRunAsAdministrator() : CollectionError
{
    public override string ToString()
    {
        return $"Could only run as administrator";
    }
}