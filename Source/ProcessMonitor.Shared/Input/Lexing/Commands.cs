using System;

namespace ProcessMonitor.Shared.Input.Lexing;

public enum Commands
{
    None = 0,
    Help,
    Create,
    Exit,
    Delete,
    Set,
    Status,
    Connect,
    Start,
    Stop,
    Get,
    Unknown
}

public static class StringExtensions
{
    public static Commands AsCommand(this ReadOnlySpan<char> type)
    {
        return type switch
        {
            "help" => Commands.Help,
            "h" => Commands.Help,
            "create" => Commands.Create,
            "exit" => Commands.Exit,
            "q" => Commands.Exit,
            "delete" => Commands.Delete,
            "del" => Commands.Delete,
            "set" => Commands.Set,
            "status" => Commands.Status,
            "stat" => Commands.Status,
            "connect" => Commands.Connect,
            "con" => Commands.Connect,
            "start" => Commands.Start,
            "stop" => Commands.Stop,
            "get" => Commands.Get,
            _ => Commands.Unknown
        };
    }
}