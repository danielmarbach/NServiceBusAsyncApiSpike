using System.Reflection;
using NServiceBus;
using NServiceBus.AutomaticSubscriptions.Config;
using NServiceBus.Features;
using NServiceBus.Unicast.Messages;
using Saunter.Generation;

namespace Infrastructure;

public sealed class AsyncApiFeature : Feature
{
    public AsyncApiFeature()
    {
        // Defaults(s =>
        // {
        //     var conventions = s.Get<Conventions>();
        //     conventions.Add(new PublishedEventsConvention());
        //     conventions.Add(new SubscribedEventsConvention());
        // });
    }

    protected override void Setup(FeatureConfigurationContext context)
    {
        var conventions = context.Settings.Get<Conventions>();

        context.Pipeline.Register(b => new ReplaceOutgoingEnclosedMessageTypeHeaderBehavior(publishedEventCache), "TODO");

        var messageMetadataRegistry = context.Settings.Get<MessageMetadataRegistry>();

        foreach (var messageMetadata in messageMetadataRegistry.GetAllMessages())
        {
            if (conventions.IsEventType(messageMetadata.MessageType))
            {
                var publishedEvent = messageMetadata.MessageType.GetCustomAttribute<PublishedEvent>();
                if (publishedEvent != null)
                {
                    publishedEventCache.Add(messageMetadata.MessageType, publishedEvent);
                }

                var subscribedEvent = messageMetadata.MessageType.GetCustomAttribute<SubscribedEvent>();
                if (subscribedEvent != null)
                {
                    subscribedEventCache.Add(messageMetadata.MessageType, subscribedEvent);
                }
            }
            else if (conventions.IsCommandType(messageMetadata.MessageType))
            {
                // TODO
            }
        }

        // TODO Check if installers are enabled
        context.RegisterStartupTask(b => new ManualSubscribe(subscribedEventCache));

        // with v8 registration will follow the regular MS DI stuff
        context.Container.ConfigureComponent<IDocumentGenerator>(
            builder => new ApiDocumentGenerator(publishedEventCache), DependencyLifecycle.SingleInstance);
    }
    
    class ManualSubscribe : FeatureStartupTask
    {
        private Dictionary<Type, SubscribedEvent> subscribedEvents;

        public ManualSubscribe(Dictionary<Type, SubscribedEvent> subscribedEvents)
        {
            this.subscribedEvents = subscribedEvents;
        }

        protected override async Task OnStart(IMessageSession session)
        {
            SubscriptionProxyGenerator? generator = null;
            // TODO concurrent?
            foreach (var (subscribedType, attribute) in subscribedEvents)
            {
                generator ??= new SubscriptionProxyGenerator();
                var messageType = generator.CreateTypeFrom($"{attribute.EventName}V{attribute.Version}");
                await session.Subscribe(messageType);
            }
        }

        protected override Task OnStop(IMessageSession session)
        {
            return Task.CompletedTask;
        }
    }

    private Dictionary<Type, PublishedEvent> publishedEventCache = new();
    private Dictionary<Type, SubscribedEvent> subscribedEventCache = new();
}