using System;

namespace ProcessMonitor.Shared.Input.Lexing;

public readonly record struct TokenSlice(string Source, int StartIndex, int Length, int Row, int Col, TokenSliceType Type);

public static class TokenSliceExtensions
{
    public static ReadOnlySpan<char> AsSpan(this TokenSlice slice)
    {
        return slice.Source.AsSpan(slice.StartIndex, slice.Length);
    }
}