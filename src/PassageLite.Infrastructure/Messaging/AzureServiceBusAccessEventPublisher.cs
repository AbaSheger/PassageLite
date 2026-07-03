using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using PassageLite.Application.Events;
using PassageLite.Application.Interfaces;

namespace PassageLite.Infrastructure.Messaging;

public class AzureServiceBusAccessEventPublisher : IAccessEventPublisher
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ServiceBusClient _serviceBusClient;
    private readonly string _accessGrantedQueueOrTopicName;

    public AzureServiceBusAccessEventPublisher(
        ServiceBusClient serviceBusClient,
        IOptions<AzureServiceBusOptions> options)
    {
        _serviceBusClient = serviceBusClient;
        _accessGrantedQueueOrTopicName = options.Value.AccessGrantedQueueOrTopicName
            ?? throw new InvalidOperationException("AzureServiceBus:AccessGrantedQueueOrTopicName is required.");
    }

    public async Task PublishAccessGrantedAsync(AccessGrantedEvent accessGrantedEvent, CancellationToken cancellationToken = default)
    {
        await using var sender = _serviceBusClient.CreateSender(_accessGrantedQueueOrTopicName);
        var message = new ServiceBusMessage(JsonSerializer.SerializeToUtf8Bytes(accessGrantedEvent, JsonSerializerOptions))
        {
            ContentType = "application/json",
            Subject = "AccessGranted",
            MessageId = accessGrantedEvent.GrantId.ToString(),
            CorrelationId = accessGrantedEvent.UserId.ToString()
        };

        message.ApplicationProperties["eventType"] = "AccessGranted";
        message.ApplicationProperties["areaId"] = accessGrantedEvent.AreaId.ToString();

        await sender.SendMessageAsync(message, cancellationToken);
    }
}
