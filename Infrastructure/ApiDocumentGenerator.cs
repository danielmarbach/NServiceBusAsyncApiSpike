using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NJsonSchema.Generation;
using NServiceBus;
using NServiceBus.Settings;
using NServiceBus.Unicast.Messages;
using Saunter;
using Saunter.AsyncApiSchema.v2;
using Saunter.Generation;
using Saunter.Generation.Filters;
using Saunter.Generation.SchemaGeneration;
using Saunter.Utils;

namespace Infrastructure;

class ApiDocumentGenerator : IDocumentGenerator
{
    private readonly Dictionary<Type, PublishedEvent> publishedEvents;

    public ApiDocumentGenerator(Dictionary<Type, PublishedEvent> publishedEvents)
    {
        this.publishedEvents = publishedEvents;
    }

    public AsyncApiDocument GenerateDocument(TypeInfo[] asyncApiTypes, AsyncApiOptions options, AsyncApiDocument prototype,
        IServiceProvider serviceProvider)
    {
        var asyncApiSchema = prototype.Clone();

        var schemaResolver = new AsyncApiSchemaResolver(asyncApiSchema, options.JsonSchemaGeneratorSettings);

        var generator = new JsonSchemaGenerator(options.JsonSchemaGeneratorSettings);
        asyncApiSchema.Channels = GenerateChannels(schemaResolver, generator);

        var filterContext = new DocumentFilterContext(asyncApiTypes, schemaResolver, generator);
        foreach (var filterType in options.DocumentFilters)
        {
            var filter = (IDocumentFilter)serviceProvider.GetRequiredService(filterType);
            filter.Apply(asyncApiSchema, filterContext);
        }

        return asyncApiSchema;
    }

    IDictionary<string, ChannelItem> GenerateChannels(AsyncApiSchemaResolver schemaResolver, JsonSchemaGenerator schemaGenerator)
    {
        var channels = new Dictionary<string, ChannelItem>();

        channels.AddRange(GenerateEventChannels(publishedEvents.Select(kvp => (kvp.Key, kvp.Value)), schemaResolver, schemaGenerator));
        return channels;
    }

    IDictionary<string, ChannelItem> GenerateEventChannels(IEnumerable<(Type Key, PublishedEvent Value)> eventTypes, AsyncApiSchemaResolver schemaResolver, JsonSchemaGenerator schemaGenerator)
    {
        var publishChannels = new Dictionary<string, ChannelItem>();
        foreach (var (eventType, attribute) in eventTypes)
        {
            // TODO: Is there a better way to handle the version?
            var operationId = $"{attribute.EventName}V{attribute.Version}";
            var subscribeOperation = new Operation
            {
                OperationId = operationId,
                Summary = string.Empty,
                Description = string.Empty,
                Message = GenerateMessageFromType(eventType, schemaResolver, schemaGenerator),
                Bindings = null,
            };

            var channelItem = new ChannelItem
            {
                Description = eventType.FullName,
                Parameters = new Dictionary<string, IParameter>(),
                Publish = null,
                Subscribe = subscribeOperation,
                Bindings = null,
                Servers = null,
            };

            publishChannels.Add(operationId, channelItem);
        }

        return publishChannels;
    }

    private static Saunter.AsyncApiSchema.v2.IMessage GenerateMessageFromType(Type payloadType, AsyncApiSchemaResolver schemaResolver, JsonSchemaGenerator jsonSchemaGenerator)
    {
        var message = new Message
        {
            Payload = jsonSchemaGenerator.Generate(payloadType, schemaResolver),
            // TODO Should this also use the operation id?
            Name = payloadType.FullName
        };

        return schemaResolver.GetMessageOrReference(message);
    }
}