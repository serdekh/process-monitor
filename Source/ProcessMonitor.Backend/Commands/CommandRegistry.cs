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

    public void Register(string route, Type handler)
    {
        _map[route] = handler;
    }

    // TODO: Handle invalid lookup
    public Type GetHandler(string route)
    {
        return _map[route];
    }
}
