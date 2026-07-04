namespace ProcessMonitor.Shared.Protocol;

public sealed class MessageEnvelope<T>
{
   public MessageType Type { get; init; }

   public T Payload { get; init; } = default!;
} 
