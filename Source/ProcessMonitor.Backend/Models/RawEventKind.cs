namespace ProcessMonitor.Backend.Models;

public enum RawEventKind
{
    Undefined,
    ContextSwitch,
    ThreadStart,
    ThreadStop,
    ProcessStart,
    ProcessStop,
    ImageLoad,
    ImageUnload,
    SyscallEnter,
    SyscallExit
}