using Microsoft.Diagnostics.Tracing;
using ProcessMonitor.Backend.Models.Errors.Collection;
using ProcessMonitor.Backend.Models.Warnings.Collection;
using ProcessMonitor.Shared.Models;
using ProcessMonitor.Shared.Models.Results;

namespace ProcessMonitor.Backend.Collection;

public delegate Result<None, CollectionError, CollectionWarning> EventHandlerFunc(TraceEvent e);