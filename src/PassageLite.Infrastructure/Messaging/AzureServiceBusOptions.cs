namespace PassageLite.Infrastructure.Messaging;

public class AzureServiceBusOptions
{
    public string? ConnectionString { get; set; }
    public string? AccessGrantedQueueOrTopicName { get; set; }
}
