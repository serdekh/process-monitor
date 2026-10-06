using System;
using System.Collections.Generic;

using ProcessMonitor.Shared.Models.Errors;

namespace ProcessMonitor.Shared.Models.Results;

public abstract record Result<T, TError, TWarning>
    where TError : Error
    where TWarning : Warning
{
    public IReadOnlyList<TWarning> Warnings { get; init; } = [];
}

public static class Result
{
    public static Success<T, TError, TWarning> Success<T, TError, TWarning>(T value, IReadOnlyList<TWarning>? warnings = null)
        where TError : Error
        where TWarning : Warning
            => new(value) { Warnings = warnings ?? [] };

    public static Failure<T, TError, TWarning> Failure<T, TError, TWarning>(TError error, IReadOnlyList<TWarning>? warnings = null)
        where TError : Error
        where TWarning : Warning
            => new(new ErrorChain<TError>(error)) { Warnings = warnings ?? [] };

    public static Failure<T, TError, TWarning> Failure<T, TError, TWarning>(TError error, ErrorChain<TError> inner, IReadOnlyList<TWarning>? warnings = null)
        where TError : Error
        where TWarning : Warning
            => new(new ErrorChain<TError>(error, inner)) { Warnings = warnings ?? [] };

    public static Success<T, TError, TWarning> AsSuccess<T, TError, TWarning>(this Result<T, TError, TWarning> result)
        where TError : Error
        where TWarning : Warning
            => (Success<T, TError, TWarning>)result;

    public static Failure<T, TError, TWarning> AsFailure<T, TError, TWarning>(this Result<T, TError, TWarning> result)
        where TError : Error
        where TWarning : Warning
            => (Failure<T, TError, TWarning>)result;

    public static bool IsSuccess<T, TError, TWarning>(this Result<T, TError, TWarning> result)
        where TError : Error
        where TWarning : Warning
            => result is Success<T, TError, TWarning>;

    public static bool IsFailure<T, TError, TWarning>(this Result<T, TError, TWarning> result)
        where TError : Error
        where TWarning : Warning
            => result is Failure<T, TError, TWarning>;

    public static void ForEachWarning<T, TError, TWarning>(this Result<T, TError, TWarning> result, Action<TWarning> predicate)
        where TError : Error
        where TWarning : Warning
    {
        foreach (var warning in result.Warnings)
        {
            predicate(warning);
        }
    }

    public static void ForEachError<T, TError, TWarning>(this Failure<T, TError, TWarning> result, Action<TError> predicate)
        where TError : Error
        where TWarning : Warning
    {
        for (var it = result.Chain; it is not null; it = it.Inner)
        {
            predicate(it.Error);
        }
    }
}

public static class ResultExtensions
{
    public static Result<U, E, W> Map<T, U, E, W>(
        this Result<T, E, W> result,
        Func<T, U> transform)
        where E : Error
        where W : Warning
    {
        return result switch
        {
            Success<T, E, W> success =>
                new Success<U, E, W>(transform(success.Value))
                {
                    Warnings = success.Warnings
                },

            Failure<T, E, W> failure =>
                new Failure<U, E, W>(failure.Chain)
                {
                    Warnings = failure.Warnings
                },

            _ => throw new InvalidOperationException()
        };
    }

    public static Result<U, E, W> Bind<T, U, E, W>(
        this Result<T, E, W> result,
        Func<T, Result<U, E, W>> next)
        where E : Error
        where W : Warning
    {
        return result switch
        {
            Success<T, E, W> success =>
                MergeWarnings(
                    success.Warnings,
                    next(success.Value)),

            Failure<T, E, W> failure =>
                new Failure<U, E, W>(failure.Chain)
                {
                    Warnings = failure.Warnings
                },

            _ => throw new InvalidOperationException()
        };
    }

    private static Result<T, E, W> MergeWarnings<T, E, W>(
        IReadOnlyList<W> previous,
        Result<T, E, W> next)
        where E : Error
        where W : Warning
    {
        return next switch
        {
            Success<T, E, W> success =>
                success with
                {
                    Warnings = [.. previous, .. success.Warnings]
                },

            Failure<T, E, W> failure =>
                failure with
                {
                    Warnings = [.. previous, .. failure.Warnings]
                },

            _ => throw new InvalidOperationException()
        };
    }
}