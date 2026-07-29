using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.Shared.Client.State;
using ProcessMonitor.Shared.Client.Input.Lexing;
using ProcessMonitor.Shared.Client.Input.Transpiling;
using ProcessMonitor.Shared.Client.Input.Interpretation;

namespace ProcessMonitor.CLI.Hosting;

public sealed class InputReaderHostedService : BackgroundService
{
    private readonly CommandLexer _lexer;
    private readonly CommandTranspiler _transpiler;
    private readonly CommandInterpreter _interpreter;
    private readonly ClientApplicationState _state;

    private readonly ILogger<InputReaderHostedService> _logger;

    public InputReaderHostedService(
        ILogger<InputReaderHostedService> logger,
        ClientApplicationState state)
    {
        _logger = logger;

        _lexer = new CommandLexer();

        _transpiler = new CommandTranspiler();

        _state = state;

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

            if (_state.LatestSnapshot is not null)
            {
                Console.WriteLine($"{_state.LatestSnapshot}");
                _state.LatestSnapshot = null;
            }
            
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