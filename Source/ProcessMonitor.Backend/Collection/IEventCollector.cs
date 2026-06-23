using System.Threading;
using System.Threading.Tasks;

namespace ProcessMonitor.Backend.Collection;

public interface IEventCollector
{
    Task RunAsync(CancellationToken ct);
}
