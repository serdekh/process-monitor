namespace ProcessMonitor.Shared.Models.Results;

public sealed record Success<T, TError, TWarning>(T Value) 
    : Result<T, TError, TWarning>
        where TError : Error
        where TWarning : Warning;