using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.ComponentModel;

// TODO: Extract the serialization code into a the contracts project
// since it is being used by both the server and the client. Consider
// renaming the contracts project to shared.
using ProcessMonitor.Backend.Serialization;

using ProcessMonitor.Contracts.Protocol;

namespace ProcessMonitor.CLI.Transport;

public sealed class CommandPipeClient : IDisposable
{
    public ProcessStartInfo BackendStartInfo { get; set; }

    private NamedPipeClientStream? _client = null;

    private IMessageSerializer _serializer;

    public bool IsConnected 
    { 
        get
        {
            return _client is null ? false : _client.IsConnected;
        } 
    }

    public CommandPipeClient(string backendFilePath, IMessageSerializer? serializer = null)
    {
        BackendStartInfo = new ProcessStartInfo
        {
            FileName = backendFilePath,
            UseShellExecute = true
        };

        _serializer = serializer is null ? new JsonMessageSerializer() : serializer;
    }
    
    public void CleanupConnection()
    {
        _client?.Dispose(); _client = null;
        Console.WriteLine("procmon: info: The current client got disconnected.");
    }

    public void Dispose()
    {
        CleanupConnection();
    }

    public async Task WriteAsync(CommandRequest request)
    {
        if (_client is null)
        {
            Console.WriteLine("procmon: error: Could not write a request. No connection was established.");
            return;
        }
 
        try 
        {
            var requestBytes = _serializer.Serialize(request);

            var lengthBytes = BitConverter.GetBytes(requestBytes.Length);

            await _client.WriteAsync(lengthBytes, 0, 4);
        
            await _client.WriteAsync(requestBytes, 0, requestBytes.Length);

            await _client.FlushAsync();
        }
        catch (Exception)
        {
            Console.WriteLine("procmon: error: could not write a request to the `Commands` pipe.");
        }
    }

    private bool TryCreateBackendProcess()
    {
        try
        {
            var process = Process.Start(BackendStartInfo);
            
            if (process is null)
            {
                Console.WriteLine("procmon: error: Could not start the backend process.");
            }

            return process is null;
        }
        catch (Win32Exception)
        {
            Console.WriteLine("procmon: error: The file was not found, access was denied or executable was corruputed.");
        }       
        catch (FileNotFoundException)
        {
            Console.WriteLine($"procmon: error: Could not find the file at {BackendStartInfo.FileName}.");
        }
        catch (ObjectDisposedException)
        {
            Console.WriteLine("procmon: error: Could not start a process that has already been disposed.");
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine("procmon: error: No process startup information was provided.");
        }
        catch (InvalidOperationException)
	    {
	        Console.WriteLine("procmon: error: No file name was provided or stream redirection failed.");
	    }	 
        catch (Exception)
        {
            Console.WriteLine("procmon: error: Unknown error.");
        }

        return false;
    }
 
    private bool TryCreateClientStream()
    {
        _client = new NamedPipeClientStream(
            ".", 
            "ProcessMonitor.Pipes.Commands", 
            PipeDirection.InOut,
            PipeOptions.Asynchronous);

        if (_client is null) 
        {
            Console.WriteLine("procmon: error: Could not create the `Commands` pipe client.");
            return false;
        }

        Console.WriteLine("procmon: info: Created the `Commands` pipe client.");
        return true;
     }
   
    public async Task<bool> ConnectAsync()
    {
        if (_client is not null) CleanupConnection();

        if (!TryCreateBackendProcess()) return false; 

        if (!TryCreateClientStream()) return false;

        try 
        { 
            if (_client is null) return false;

            await _client.ConnectAsync();
                
            Console.WriteLine("procmon: info:  Connected to the `Commands` pipe");
        }
        catch (InvalidOperationException) 
        {
            Console.WriteLine("procmon: error: Could not connect to `Commands` pipe: already connected.");
        }            

        return IsConnected;
    }
}
