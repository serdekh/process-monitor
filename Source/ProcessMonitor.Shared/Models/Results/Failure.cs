namespace ProcessMonitor.Shared.Models.Results;

public sealed record Failure<T, TError, TWarning>(ErrorChain<TError> Error)
    : Result<T, TError, TWarning>
        where TError : Error
        where TWarning : Warning;

