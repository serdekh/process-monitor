using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;

namespace ProcessMonitor.Backend.Models;

public enum RawEventKind
{
    Undefined,
    ContextSwitch,
    ThreadStart,
    ThreadStop,
    ThreadDCStart,
    ThreadDCEnd,
    SyscallEnter,
    SyscallExit
}

public static class RawEventKindExtensions
{
    public static RawEventKind ToRawEventKind(this TraceEvent e)
    {
        if (e is CSwitchTraceData)
            return RawEventKind.ContextSwitch;

        if (e.EventName == "PerfInfo/SysClEnter" || (int)e.ID == 51)
            return RawEventKind.SyscallEnter;

        if (e.EventName == "PerfInfo/SysClExit" || (int)e.ID == 52)
            return RawEventKind.SyscallExit;

        if (e is ThreadTraceData)
        {
            return (int)e.Opcode switch
            {
                1 => RawEventKind.ThreadStart,
                2 => RawEventKind.ThreadStop,
                3 => RawEventKind.ThreadDCStart,
                4 => RawEventKind.ThreadDCEnd,
                _ => RawEventKind.Undefined
            };
        }

        return RawEventKind.Undefined;
    }

    public static string AsString(this RawEventKind kind)
    {
        return kind switch
        {
            RawEventKind.ContextSwitch => "Context Switch",
            RawEventKind.ThreadStart => "Thread Start",
            RawEventKind.ThreadStop => "Thread Stop",
            RawEventKind.ThreadDCStart => "Thread DC Start",
            RawEventKind.ThreadDCEnd => "Thread DC End",
            RawEventKind.SyscallEnter => "Syscall Enter",
            RawEventKind.SyscallExit => "Syscall Exit",
            RawEventKind.Undefined => "(undefined)",
            _ => "(unknown)"
        };
    }
}