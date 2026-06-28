using System;
using System.IO;
using System.IO.Pipes;
using System.Diagnostics;
using System.Threading.Tasks;
using System.ComponentModel;

using ProcessMonitor.Contracts.Protocol;

namespace ProcessMonitor.CLI.Transport;

public sealed class CommandPipeClient : IDisposable
{
    public string BackendFilePath { get; set; } 

    public ProcessStartInfo BackendStartInfo { get; set; }

    private NamedPipeClientStream? _client = null;

    public bool IsConnected 
    { 
        get
        {
            return _client is null ? false : _client.IsConnected;
        } 
    }

    public CommandPipeClient(string backendFilePath)
    {
        BackendFilePath = backendFilePath;
        BackendStartInfo = new ProcessStartInfo() 
        {
            FileName = BackendFilePath,
            UseShellExecute = true
        };
    }
    
    public void CleanupConnection()
    {
        _client?.Dispose(); _client = null;
    }

    public void Dispose()
    {
        CleanupConnection();
    }

    // TODO: Refactor this code to properly behave in case of errors
    public async Task Write()
    {
        if (_client is null) return;
 
        for (int i = 0; i < 5; i++) 
        {
            try
            {
                var message = "Hello from the client! IPC IS ACHIEVED!";

                var messageBytes = System.Text.Encoding.UTF8.GetBytes(message);

                var lengthBytes = BitConverter.GetBytes(messageBytes.Length);

                await _client.WriteAsync(lengthBytes, 0, 4);

                await _client.WriteAsync(messageBytes, 0, messageBytes.Length);

                await _client.FlushAsync();
            } 
            catch (Exception)
            {
                Console.WriteLine("procmon: error: the server has disconnected or the pipe has been corrupted."); 
            }
        }
    }

    // TODO: Move code that creates a process into a separate class
    public async Task ConnectAsync()
    {
        if (_client is not null) CleanupConnection();

        Console.WriteLine("procmon: info:  The previous connection is closed.");

        try 
        {
            var process = Process.Start(BackendStartInfo);

            if (process is null)
            {
                Console.WriteLine("procmon: error: Could not start the backend process.");
                return;
            }

            Console.WriteLine("procmon: info:  Started the backend process.");

            _client = new NamedPipeClientStream(
                ".", 
                "ProcessMonitor.Pipes.Commands", 
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            if (_client is null) 
            {
                Console.WriteLine("procmon: error: Could not create the `Commands` pipe client.");
                return;
            }

            Console.WriteLine("procmon: info:  Created the `Commands` pipe client.");

            try 
            {
                await _client.ConnectAsync();
                Console.WriteLine("procmon: info:  Connected to the `Commands` pipe");
            }
            catch (InvalidOperationException ) 
            {
                Console.WriteLine("procmon: error: Could not connect to `Commands` pipe: already connected.");
                return;
            }            
        }
        catch (Win32Exception)
        {
            Console.WriteLine("procmon: error: System exception: could not open a file.");   
            return;
        }
        catch (Exception e)
        {
           Console.WriteLine(e);
           Console.WriteLine("procmon: error: fatal");
           return;
        }
    }
}
