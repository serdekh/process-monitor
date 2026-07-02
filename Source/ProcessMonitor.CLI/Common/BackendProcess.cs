using System;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;

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
            return _backend is not null; 
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
                Console.WriteLine("- Attempt to create an existing process: ignore");
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
        if (HasError) return "No error";

        switch (_error)
        {
            case Win32Exception:            return "The file was not found, access was denied or executable was corruputed";
            case FileNotFoundException:     return $"The file {_startInfo.FileName} was not found";
            case ObjectDisposedException:   return "Could not start a backend process that has been disposed";
            case ArgumentNullException:     return "No process start-up information was provided";
            case InvalidOperationException: return "No file name was provided or stream redirection failed";
            
            default: return "Unknown error";     
        }
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
        this.Kill();
    }
}
