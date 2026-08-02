using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

namespace ProcessMonitor.Shared.Client.Hosting.Services;

public abstract class InputReaderService : BackgroundService
{
    public abstract Task<Exception?> ReadLineAsync(CancellationToken ct);
}