using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ProcessMonitor.Shared.Input.Lexing;

public sealed class CommandLexer
{
    public List<TokenSlice> Tokens { get; set; } = new(32);

    public async Task<Exception?> LexInput(CancellationToken ct, bool clear = true)
    {
        if (ct.IsCancellationRequested) return new OperationCanceledException();

        string? input = null;
        
        Console.Write("procmon>");
        
        try
        {
            await Task.Run(() => input = Console.ReadLine(), ct);
        }
        catch (Exception ex)
        {
            return ex;
        }

        if (string.IsNullOrWhiteSpace(input)) return null;

        return LexString(input, [' '], clear);
    }

    private void UpdateLocation(char c, ref int row, ref int col)
    {
        if (c == '\n')
        {
            row++;
            col = 1;
        }
        else if (c != '\r') 
        {
            col++;
        }
    }

    public Exception? LexString(string source, char[] delimeters, bool clear = true)
    {
        if (clear) Tokens.Clear();

        if (string.IsNullOrEmpty(source)) return null;

        var sourceSpan = source.AsSpan();

        var row = 0;
        var col = 0;

        var tokenRow = 0;
        var tokenCol = 0;

        var startIndex = 0;

        for (int i = 0; i <= sourceSpan.Length; i++)
        {
            if (i == sourceSpan.Length || delimeters.Contains(sourceSpan[i]))
            {
                var length = i - startIndex;

                if (length > 0)
                {
                    var lexeme = source.AsSpan(startIndex, length);

                    TokenSliceType sliceType;

                    if (lexeme.AsCommand() != Commands.Unknown)
                    {
                        sliceType = TokenSliceType.Command;
                    }
                    // TODO: replace memory expensive tryparse call with a custom
                    // extension method that check whether a char span is a numeric literal
                    else if (int.TryParse(lexeme, out int result))
                    {
                        sliceType = TokenSliceType.NumberLiteral;
                    }
                    else
                    {
                        return new FormatException($"'{lexeme}' was not recognized as a command or a number literal");
                    }

                    var slice = new TokenSlice(source, startIndex, length, tokenRow, tokenCol, sliceType);

                    Tokens.Add(slice);
                }
                
                startIndex = i + 1;

                if (i < sourceSpan.Length)
                {
                    UpdateLocation(sourceSpan[i], ref row, ref col);
                }

                tokenRow = row;
                tokenCol = col;
            }
            else
            {
                UpdateLocation(sourceSpan[i], ref row, ref col);
            }
        }

        return null;
    }
}