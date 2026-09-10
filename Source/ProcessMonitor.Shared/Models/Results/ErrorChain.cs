namespace ProcessMonitor.Shared.Models.Results;

public sealed record ErrorChain<TError>(
    TError Error,
    ErrorChain<TError>? Inner = null) 
    where TError : Error
{
    public ErrorChain<TError> CausedBy(TError error) => new(error, this);
}