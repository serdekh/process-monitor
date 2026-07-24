namespace ProcessMonitor.Shared.Input.Lexing;

public readonly record struct TokenSlice(string Source, int StartIndex, int Length, int Row, int Col, TokenSliceType Type);