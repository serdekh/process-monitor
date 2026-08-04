using System;
using System.IO;
using System.Collections.Generic;

using ProcessMonitor.Shared.Client.State;

namespace ProcessMonitor.Shared.Client.Input.Args;

public sealed class ArgsParser
{
    private delegate (int, Exception?) ParserFunc(string[] args, int cursor);

    private readonly Dictionary<string, ParserFunc> _parsers;

    public ClientApplicationConfiguration Configuration { get; set; } = new();

    private readonly static int _maxIterationCount = 1000;

    public ArgsParser()
    {
        _parsers = new Dictionary<string, ParserFunc>()
        {
            ["--pid"] = ParsePidFlag,
            ["--path"] = ParsePathFlag
        };
    }

    private (int, Exception?) ParsePidFlag(string[] args, int cursor)
    {
        if (cursor >= args.Length || cursor < 0) return (cursor, new ArgumentException("Index out of range during flag parsing"));

        if (cursor + 1 == args.Length) return (cursor, new ArgumentException("Not enough arguments for the 'pid' flag.\nUsage:\t--pid <integer>"));

        try
        {
            var pid = int.Parse(args[cursor + 1]);

            Configuration.ProcessId = pid;

            return (cursor + 2, null);
        }
        catch (Exception ex)
        {
            return (cursor + 2, new ArgumentException($"Could not parse the argument of the 'pid' flag:{ex.Message}\nUsage:\t--pid <integer>"));
        }
    }

    private (int, Exception?) ParsePathFlag(string[] args, int cursor)
    {
        if (cursor >= args.Length || cursor < 0) return (cursor, new ArgumentException("Index out of range during flag parsing"));

        if (cursor + 1 == args.Length) return (cursor, new ArgumentException("Not enough arguments for the 'path' flag.\nUsage:\t--path <filepath>"));

        if (!File.Exists(args[cursor + 1])) return (cursor + 2, new ArgumentException($"Could not find the '{args[cursor + 1]}' file"));

        Configuration.ServerFilepath = args[cursor + 1];

        return (cursor + 2, null);
    }

    private (int, Exception?) ParseNoneFlag(string[] args, int cursor)
    {
        return (cursor + 1, null);
    }

    private (ParserFunc, Exception?) MapToParser(string[] args, int cursor)
    {
        if (!_parsers.TryGetValue(args[cursor], out var parser))
        {
            return (ParseNoneFlag, new ArgumentException($"'{args[cursor]}' was not recognized as a flag"));
        }

        return (parser, null);
    }

    public Exception? Parse(string[] args)
    {
        var count = 0;
        var cursor = 0;

        while (cursor < args.Length && count < _maxIterationCount)
        {
            (var parser, var mappingException) = MapToParser(args, cursor);

            if (mappingException is not null) return mappingException;

            (var newCursor, var parsingException) = parser(args, cursor);

            if (parsingException is not null) return parsingException;

            cursor = newCursor; count++;
        }

        if (cursor >= _maxIterationCount) 
            return new ArgumentOutOfRangeException($"The amount of parsing iterations exceeded the threshold of '{_maxIterationCount}' loops");

        return null;
    }
}