using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using ProcessMonitor.Shared.Client.State;

namespace ProcessMonitor.Shared.Client.Hosting.Services;

public abstract class RendererService : BackgroundService
{
    public abstract Task<Exception?> Render(ClientApplicationState state, CancellationToken ct);
}