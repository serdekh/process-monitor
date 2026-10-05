namespace ProcessMonitor.Shared.Models.Warnings.Serialization;

public record SerializationJsonIsNullWarning : SerializationWarning
{
    public override string ToString() => "Input data is null because it is a valid JSON";
}