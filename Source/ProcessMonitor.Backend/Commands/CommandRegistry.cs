using System;
using System.Collections.Generic;

using ProcessMonitor.Backend.Commands.Handlers;

namespace ProcessMonitor.Backend.Commands;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, Type> _map;

    public CommandRegistry()
    {
        _map = new Dictionary<string, Type>
        {
            ["monitoring/post"] = typeof(StartMonitoringHandler),
            ["monitoring/delete"] = typeof(StopMonitoringHandler)
        };
    }

    public Exception? Register(string route, Type handler)
    {
        try
        {
            _map[route] = handler;
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    public (Type?, Exception?) GetHandler(string route)
    {
        try
        {
            return (_map[route], null);
        }
        catch (Exception ex)
        {
            return (null, ex);
        }
    }
}
