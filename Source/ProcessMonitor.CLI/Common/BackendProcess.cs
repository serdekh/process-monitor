using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;

using ProcessMonitor.CLI.State;

namespace ProcessMonitor.CLI.Common;

// TODO: Consider making this class 'CLI project independent' to allow 
// moving it upper into the 'Shared' project. This will let the future
// 'WPF-based' client implementation to use the 'CommandInterpreter' and
// 'CommandInterpreterState' classes thus eliminating the need of having
// two interpreters for each client
public sealed class BackendProcess : IAsyncDisposable
{
    private Process? _backend = null;

    private readonly ProcessStartInfo _startInfo;

    private EventHandler? _onExit = null;

    public string Path => _startInfo.FileName;

    public bool HasExited
    {
        get
        {
            if (_backend is null) return false;

            _backend.Refresh();

            return _backend.HasExited;
        }
    }

    public bool IsRunning
    { 
        get
        {
            _backend?.Refresh();
            return _backend is not null && !_backend.HasExited; 
        }
    }

    public BackendProcess(IOptions<RuntimeState> options)
    {
        _startInfo = new ProcessStartInfo
        {
            FileName = options.Value.BackendProcessFilePath,
            UseShellExecute = true,
            Verb = "runas"
        };
    }

    public void AddOnExitHandler(EventHandler onExit)
    {
        _onExit += onExit;
    }

    public Exception? TryCreate()
    {
        if (_backend is not null)
        {
            if (HasExited)
            {
                _backend.Dispose();
                _backend = null;
            }
            else
            {
                return null;
            }
        }

        try
        {
            _backend = Process.Start(_startInfo);
   
            if (_backend is null) return null;
            
            _backend.EnableRaisingEvents = true;

            if (_onExit is not null) _backend.Exited += _onExit;

            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public string GetErrorString(Exception? ex)
    {
        if (ex is null) return "No error";

        return ex switch
        {
            Win32Exception => "The file was not found, access was denied or executable was corruputed",
            FileNotFoundException => $"The file {_startInfo.FileName} was not found",
            ObjectDisposedException => "Could not start a backend process that has been disposed",
            ArgumentNullException => "No process start-up information was provided",
            ArgumentOutOfRangeException => "The cancellation time delay was out of rage",
            InvalidOperationException => "No file name was provided or stream redirection failed",
            _ => "Unknown error",
        };
    }

    public async Task<Exception?> KillAsync(TimeSpan delay)
    {
        if (_backend is null || HasExited) return null;

        try
        {
            var taskKillInfo = new ProcessStartInfo
            {
                FileName = "taskkill.exe",
                Arguments = $"/PID {_backend.Id} /T", 
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using var killer = Process.Start(taskKillInfo);

            killer?.WaitForExit();

            using var cts = new CancellationTokenSource(delay);
            await _backend.WaitForExitAsync(cts.Token);
            
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public async Task<Exception?> KillAsync() => await KillAsync(TimeSpan.FromSeconds(3));

    public async ValueTask DisposeAsync() => await KillAsync();
}
