using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ProcessMonitor.CLI.Input;

namespace ProcessMonitor.CLI.Hosting;

public sealed class InputReaderHostedService : BackgroundService
{
    private ILogger<InputReaderHostedService> _logger;

    private ConsoleInputReader _inputReader;

    public InputReaderHostedService(
        ILogger<InputReaderHostedService> logger,
        ConsoleInputReader inputReader)
    {
        _logger = logger;
        _inputReader = inputReader; 
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
        {
            _logger.LogError("[Host][InputReader]: Could not start reading user input: cancellation requested.");
            return;
        }

        _logger.LogInformation("[Host][InputReader]: Starting...");

        await _inputReader.ReadAsync(ct);

        _logger.LogInformation("[Host][InputReader]: Terminating...");
    }
}