using System;
using System.Collections.Generic;

namespace ProcessMonitor.Backend.Commands;

public sealed class CommandRegistry
{
    private readonly Dictionary<string, Type> _map = new();

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
