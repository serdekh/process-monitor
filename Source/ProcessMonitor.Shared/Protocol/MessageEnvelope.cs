namespace ProcessMonitor.Shared.Protocol;

public sealed class MessageEnvelope<T>
{
   public MessageType Type { get; set; }

   public T Payload { get; set; } = default!;
} 
