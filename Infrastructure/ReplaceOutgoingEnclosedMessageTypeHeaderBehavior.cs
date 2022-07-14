using NServiceBus;
using NServiceBus.Pipeline;

namespace Infrastructure;

class ReplaceOutgoingEnclosedMessageTypeHeaderBehavior : IBehavior<IOutgoingPhysicalMessageContext,
    IOutgoingPhysicalMessageContext>
{
    private Dictionary<Type, PublishedEvent> publishedEventCache;

    public ReplaceOutgoingEnclosedMessageTypeHeaderBehavior(Dictionary<Type, PublishedEvent> publishedEventCache)
    {
        this.publishedEventCache = publishedEventCache;
    }

    public Task Invoke(IOutgoingPhysicalMessageContext context, Func<IOutgoingPhysicalMessageContext, Task> next)
    {
        var logicalMessage = context.Extensions.Get<OutgoingLogicalMessage>();
        if (publishedEventCache.TryGetValue(logicalMessage.MessageType, out var publishedEvent))
        {
            // very blunt and might break with certain transports
            context.Headers[Headers.EnclosedMessageTypes] = $"{publishedEvent.EventName}V{publishedEvent.Version}";
        }

        return next(context);
    }
}