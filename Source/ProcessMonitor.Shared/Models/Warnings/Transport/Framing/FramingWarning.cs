using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Shared.Models.Warnings.Transport.Framing;

public abstract record FramingWarning : Warning
{
    public override string ToString() => $"{base.ToString()}";
}