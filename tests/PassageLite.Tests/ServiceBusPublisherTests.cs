using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using PassageLite.Application.Events;
using PassageLite.Infrastructure.Messaging;
using System.Text.Json;
using Xunit;

namespace PassageLite.Tests;

public class ServiceBusPublisherTests : IAsyncLifetime
{
    private static readonly string? ConnectionString =
        Environment.GetEnvironmentVariable("PASSAGELITE_SERVICEBUS_TEST_CONNECTION");

    private const string QueueName = "access-granted";

    private ServiceBusClient? _client;
    private ServiceBusReceiver? _receiver;

    public Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString)) return Task.CompletedTask;

        _client = new ServiceBusClient(ConnectionString);
        _receiver = _client.CreateReceiver(QueueName, new ServiceBusReceiverOptions
        {
            ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete
        });

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_receiver is not null) await _receiver.DisposeAsync();
        if (_client is not null) await _client.DisposeAsync();
    }

    [SkippableFact]
    public async Task PublishAccessGrantedAsync_SendsMessageToQueue()
    {
        Skip.If(string.IsNullOrWhiteSpace(ConnectionString), "PASSAGELITE_SERVICEBUS_TEST_CONNECTION not set; skipping Service Bus test.");

        var options = Options.Create(new AzureServiceBusOptions
        {
            ConnectionString = ConnectionString,
            AccessGrantedQueueOrTopicName = QueueName
        });

        var publisher = new AzureServiceBusAccessEventPublisher(_client!, options);

        var evt = new AccessGrantedEvent(
            GrantId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            AreaId: Guid.NewGuid(),
            AreaName: "Test Area",
            ValidFrom: DateTime.UtcNow.AddDays(-1),
            ValidTo: DateTime.UtcNow.AddDays(1),
            OccurredAt: DateTime.UtcNow
        );

        await publisher.PublishAccessGrantedAsync(evt);

        var received = await _receiver!.ReceiveMessageAsync(maxWaitTime: TimeSpan.FromSeconds(15));

        Assert.NotNull(received);
        Assert.Equal(evt.GrantId.ToString(), received.MessageId);
        Assert.Equal("AccessGranted", received.Subject);
        Assert.Equal("application/json", received.ContentType);
        Assert.Equal("AccessGranted", received.ApplicationProperties["eventType"].ToString());
        Assert.Equal(evt.AreaId.ToString(), received.ApplicationProperties["areaId"].ToString());

        var body = JsonSerializer.Deserialize<AccessGrantedEvent>(
            received.Body.ToArray(),
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(body);
        Assert.Equal(evt.GrantId, body!.GrantId);
        Assert.Equal(evt.UserId, body.UserId);
        Assert.Equal("Test Area", body.AreaName);
    }
}
