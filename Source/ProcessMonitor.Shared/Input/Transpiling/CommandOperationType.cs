namespace ProcessMonitor.Shared.Input.Transpiling;

public enum CommandOperationType
{
    PrintHelp,
    CreateBackendProcess,
    Exit,
    KillBackendProcess,
    SetProcessId,
    PrintStatus,
    ConnectToBackendProcess,
    PrintRuntimeConfig,
    SendRequest,    
    Unknown,
}