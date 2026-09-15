using System;
using System.Collections.Generic;

namespace ProcessMonitor.Shared.Models.Results;

public abstract record Result<T, TError, TWarning>
    where TError : Error
    where TWarning : Warning
{
    public IReadOnlyList<TWarning> Warnings { get; init; } = [];
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