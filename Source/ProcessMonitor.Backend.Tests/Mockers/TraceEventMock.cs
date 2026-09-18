using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Tracing;

namespace ProcessMonitor.Backend.Tests.Mockers;

public class TraceEventMock : TraceEvent, IDisposable
{
    private const int TRACE_EVENT_HEADER_NATIVE_BUFFER_SIZE = 64;
    private const int TRACE_EVENT_HEADER_PROCESS_ID_OFFSET = 12;

    private IntPtr _nativeBuffer = IntPtr.Zero;
    private bool _disposed;

    public int EventId { get; set; }
    public int EventTask { get; set; }
    public int EventTaskGuid { get; set; } // Fixed type to Guid if needed, keeping matching previous setup
    public int OpCode { get; set; }
    public string OpCodeName { get; set; }
    public Guid EventProviderGuid { get; set; }
    public string EventProviderName { get; set; }

    public TraceEventMock(
        int processId = -1,
        int eventID = -1, 
        int task = -1, 
        string taskName = "", 
        Guid taskGuid = default, 
        int opcode = -1, 
        string opcodeName = "", 
        Guid providerGuid = default, 
        string providerName = "") : base(eventID, task, taskName, taskGuid, opcode, opcodeName, providerGuid, providerName)
    {
        EventId = eventID;
        EventTask = task;
        OpCode = opcode;
        OpCodeName = opcodeName;
        EventProviderGuid = providerGuid;
        EventProviderName = providerName;

        InitializeNativeProcessId(processId);
    }

    private unsafe void InitializeNativeProcessId(int processId)
    {
        var eventRecordField = typeof(TraceEvent).GetField("eventRecord", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (eventRecordField != null)
        {
            _nativeBuffer = Marshal.AllocHGlobal(TRACE_EVENT_HEADER_NATIVE_BUFFER_SIZE);
            
            Unsafe.InitBlockUnaligned((void*)_nativeBuffer, 0, 64);
            
            byte* ptr = (byte*)_nativeBuffer;

            // The Windows EVENT_HEADER structure layout is fixed:
            // Offset 0: Size (2 bytes) + HeaderType (2 bytes)
            // Offset 4: Flags (2 bytes) + EventProperty (2 bytes)
            // Offset 8: ThreadId (4 bytes)
            // Offset 12: ProcessId (4 bytes) 
            int* processIdDest = (int*)(ptr + TRACE_EVENT_HEADER_PROCESS_ID_OFFSET);
            *processIdDest = processId;
            
            eventRecordField.SetValue(this, _nativeBuffer);
        }
        else
        {
            // Fallback for isolated modern instances using internal variables
            var processIdField = typeof(TraceEvent).GetField("m_processID", BindingFlags.NonPublic | BindingFlags.Instance) 
                                ?? typeof(TraceEvent).GetField("_processId", BindingFlags.NonPublic | BindingFlags.Instance);
            
            processIdField?.SetValue(this, processId);
        }
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (_nativeBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_nativeBuffer);
                _nativeBuffer = IntPtr.Zero;
            }
            _disposed = true;
        }
    }

    ~TraceEventMock()
    {
        Dispose(false);
    }

    public override string[] PayloadNames => [];
    protected override Delegate? Target { get; set; } = null;
    public override object PayloadValue(int index) => new();
}
