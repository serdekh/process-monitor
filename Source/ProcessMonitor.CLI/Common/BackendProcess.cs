using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ProcessMonitor.CLI.Common;

public sealed class BackendProcess : IAsyncDisposable
{
    private Process? _backend = null;

    private Exception? _error = null;

    private readonly ProcessStartInfo _startInfo;

    private EventHandler? _onExit = null;

    public string Path 
    { 
        get
        {
            return _startInfo.FileName;
        } 
    } 

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

    public bool HasError
    {
        get
        {
            return _error is not null;
        }
    }

    public BackendProcess(string path)
    {
        _startInfo = new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
            Verb = "runas"
        };
    }

    public void AddOnExitHandler(EventHandler onExit)
    {
        _onExit = onExit;
    }

    public bool Create()
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
                return true;
            }
        }

        _error = null;

        try
        {
            _backend = Process.Start(_startInfo);
   
            if (_backend is not null)
            {
                _backend.EnableRaisingEvents = true;

                if (_onExit is not null) _backend.Exited += _onExit;
            }
        }
        catch (Exception ex)
        {
            _error = ex;
        }

        return _error is null;
    }

    public string GetErrorString()
    {
        if (!HasError) return "No error";

        return _error switch
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

    public async Task KillAsync(TimeSpan delay)
    {
        if (_backend is null || HasExited) return;

        _error = null;

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
        }
        catch (Exception ex)
        {
            if (ex is not OperationCanceledException) 
            {
                _error = ex;
            }
        }
    }

    public async Task KillAsync()
    {
        await KillAsync(TimeSpan.FromSeconds(3));
    }

    public async ValueTask DisposeAsync()
    {
        await KillAsync();
    }
}
