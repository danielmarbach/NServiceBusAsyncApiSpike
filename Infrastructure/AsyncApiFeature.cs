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

        var messageMetadataRegistry = context.Settings.Get<MessageMetadataRegistry>();

        TypeProxyGenerator proxyGenerator = new TypeProxyGenerator();

        foreach (var messageMetadata in messageMetadataRegistry.GetAllMessages())
        {
            if (conventions.IsEventType(messageMetadata.MessageType))
            {
                var publishedEvent = messageMetadata.MessageType.GetCustomAttribute<PublishedEvent>();
                if (publishedEvent != null)
                {
                    publishedEventCache.Add(messageMetadata.MessageType, proxyGenerator.CreateTypeFrom($"{publishedEvent.EventName}V{publishedEvent.Version}"));
                }

                var subscribedEvent = messageMetadata.MessageType.GetCustomAttribute<SubscribedEvent>();
                if (subscribedEvent != null)
                {
                    var subscribedType = proxyGenerator.CreateTypeFrom($"{subscribedEvent.EventName}V{subscribedEvent.Version}");
                    subscribedEventCache.Add(subscribedType.FullName, (SubscribedType: subscribedType, ActualType: messageMetadata.MessageType));
                }
            }
            else if (conventions.IsCommandType(messageMetadata.MessageType))
            {
                // TODO
            }
        }

        context.RegisterStartupTask(b => new ManualSubscribe(subscribedEventCache.Values.Select(x => x.SubscribedType).ToArray()));

        context.Pipeline.Register(b => new ReplaceOutgoingEnclosedMessageTypeHeaderBehavior(publishedEventCache), "TODO");
        context.Pipeline.Register(b => new ReplaceMulticastRoutingBehavior(publishedEventCache), "TODO");
        context.Pipeline.Register(b => new ReplaceIncomingEnclosedMessageTypeHeaderBehavior(subscribedEventCache), "TODO");

        // with v8 registration will follow the regular MS DI stuff
        context.Container.ConfigureComponent<IDocumentGenerator>(
            builder => new ApiDocumentGenerator(publishedEventCache), DependencyLifecycle.SingleInstance);
    }
    
    class ManualSubscribe : FeatureStartupTask
    {
        private Type[] subscribedEvents;

        public ManualSubscribe(Type[] subscribedEvents)
        {
            this.subscribedEvents = subscribedEvents;
        }

        protected override Task OnStart(IMessageSession session)
        {
            return Task.WhenAll(subscribedEvents.Select(subscribedEvent => session.Subscribe(subscribedEvent)));
        }

        protected override Task OnStop(IMessageSession session)
        {
            return Task.CompletedTask;
        }
    }

    private Dictionary<Type, Type> publishedEventCache = new();
    private Dictionary<string, (Type SubscribedType, Type ActualType)> subscribedEventCache = new();
}