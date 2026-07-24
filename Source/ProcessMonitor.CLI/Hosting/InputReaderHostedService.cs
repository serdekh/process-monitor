using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.CLI.Common;
using ProcessMonitor.CLI.Input.Interpretation;

using ProcessMonitor.Shared.Input.Lexing;
using ProcessMonitor.Shared.Serialization;
using ProcessMonitor.Shared.Input.Transpiling;
using ProcessMonitor.Shared.Transport.Framing;

namespace ProcessMonitor.CLI.Hosting;

public sealed class InputReaderHostedService : BackgroundService
{
    private readonly CommandLexer _lexer;
    private readonly CommandTranspiler _transpiler;
    private readonly CommandInterpreter _interpreter;
    private readonly CommandInterpreterState _state;

    private readonly ILogger<InputReaderHostedService> _logger;

    public InputReaderHostedService(
        ILogger<InputReaderHostedService> logger,
        BackendProcess backend, 
        IFrameWriter writer,
        IFrameReader reader,
        IMessageSerializer serializer)
    {
        _logger = logger;

        _lexer = new CommandLexer();

        _transpiler = new CommandTranspiler();

        _state = new CommandInterpreterState(backend, writer, reader, serializer);

        _interpreter = new CommandInterpreter(_state);
    }

    public async Task<Exception?> ReadLineAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return new OperationCanceledException();

        var lexingException = await _lexer.LexInput(ct);

        if (lexingException is not null) return lexingException;

        var transpilingException = _transpiler.Transpile(_lexer.Tokens);

        if (transpilingException is not null) return transpilingException;
        
        return await Task.Run(async () =>
        {
            var interpretationException = await _interpreter.Interpret(_transpiler.Operations);

            if (interpretationException is not null) return interpretationException;

            foreach (var telemetry in _state.Telemetry)
            {
                Console.WriteLine($"{telemetry}");
            }

            _state.Telemetry.Clear();
            
            return null;
        });
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogDebug("[Host][InputReader]: Could not start reading user input: cancellation requested.");
            return;
        }

        _logger.LogDebug("[Host][InputReader]: Starting...");

        _state.CancellationToken = ct;

        while (!ct.IsCancellationRequested)
        {
            var readingException = await ReadLineAsync(ct);

            if (readingException is not null)
            {
                Console.WriteLine($"procmon: error: could not run the command(s): {readingException.Message}\n\tRun 'h' or 'help' to get a list of the availiable commands");
                continue;
            }

            if (_state.Out.Length != 0) Console.WriteLine(_state.Out); _state.Out.Clear();
        }

        _logger.LogDebug("[Host][InputReader]: Terminating...");
    }
}