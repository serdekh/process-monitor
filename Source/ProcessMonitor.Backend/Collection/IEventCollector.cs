using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public interface IEventCollector
{
    Task<Result<None, CollectionError, CollectionWarning>> RunAsync(CancellationToken ct);
}