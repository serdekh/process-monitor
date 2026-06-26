using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;

using ProcessMonitor.Backend.Hosting;

namespace ProcessMonitor.Backend;

internal class Program
{
    public static async Task Main(string[] args)
    { 
        var host = ProcessMonitorHostBuilder 
            .Create(args)
            .Build();

        await host.RunAsync();
    }
}
