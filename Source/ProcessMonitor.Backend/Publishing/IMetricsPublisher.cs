using System.Threading;
using System.Threading.Tasks;

using ProcessMonitor.Shared.Snapshots;

namespace ProcessMonitor.Backend.Publishing;

public interface IMetricsPublisher 
{
    Task PublishAsync(ProcessMetricsSnapshot snapshot, CancellationToken ct);
}
