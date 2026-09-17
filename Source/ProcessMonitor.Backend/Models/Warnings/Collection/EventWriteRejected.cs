namespace ProcessMonitor.Backend.Models.Warnings.Collection;

public record EventWriteRejected(RawEventKind Kind) : CollectionWarning
{
    public override string ToString()
    {
        return $"Input channel rejected {Kind.AsString()} event";
    }
}