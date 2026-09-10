using System.Text;
using RabbitMQ.Client;
using RestaurantMenu.Ordering.Application.Abstractions;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public sealed class RabbitMqIntegrationEventPublisher(IRabbitMqConnection connection,
    MessagingOptions options) : IIntegrationEventPublisher
{
    public async Task PublishAsync(IntegrationEventEnvelope envelope,
        CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(true, cancellationToken);
        await channel.ExchangeDeclareAsync(options.ExchangeName, ExchangeType.Topic,
            durable: true, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = envelope.Id.ToString("D"),
            Type = envelope.Name,
            Timestamp = new AmqpTimestamp(envelope.OccurredAtUtc.ToUnixTimeSeconds()),
            Headers = new Dictionary<string, object?>
            {
                ["event-version"] = envelope.Version,
                ["aggregate-id"] = envelope.AggregateId.ToString("D"),
                ["aggregate-version"] = envelope.AggregateVersion
            }
        };
        await channel.BasicPublishAsync(options.ExchangeName, envelope.Name, mandatory: false,
            basicProperties: properties, body: Encoding.UTF8.GetBytes(envelope.Payload),
            cancellationToken: cancellationToken);
    }
}
