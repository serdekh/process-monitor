using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;

namespace ProcessMonitor.CLI.Common;

public sealed class BackendProcess : IDisposable
{
    private Process? _backend = null;

    private Exception? _error = null;

    private ProcessStartInfo _startInfo;

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

    public bool IsCreated 
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
            UseShellExecute = true
        };
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
            InvalidOperationException => "No file name was provided or stream redirection failed",
            _ => "Unknown error",
        };
    }

    public void Kill()
    {
        if (_backend is null) return;

        _backend.Kill();
        _backend.WaitForExit();
        _backend = null;
    }

    public void Dispose()
    {
        Kill();
    }
}
