using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Infrastructure.Messaging;

namespace RestaurantMenu.Notifications.Infrastructure;

public sealed class RabbitMqNotificationConsumerWorker(IRabbitMqConnection connection,
    IServiceScopeFactory scopeFactory, MessagingOptions messaging,
    NotificationMessagingOptions options, ILogger<RabbitMqNotificationConsumerWorker> logger)
    : BackgroundService
{
    private static readonly Action<ILogger, Exception?> LogConsumerFailure =
        LoggerMessage.Define(LogLevel.Warning, new EventId(4201, "NotificationConsumerFailed"),
            "The notification consumer stopped; it will reconnect.");
    private static readonly Action<ILogger, string, Exception?> LogRejectedMessage =
        LoggerMessage.Define<string>(LogLevel.Warning,
            new EventId(4202, "NotificationMessageRejected"),
            "A malformed notification message was rejected from {RoutingKey}.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ConsumeAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            { break; }
            catch (Exception exception)
            {
                LogConsumerFailure(logger, exception);
                await Task.Delay(options.RecoveryDelay, stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        await using var channel = await connection.CreateChannelAsync(false, cancellationToken);
        var deadExchange = $"{messaging.ExchangeName}.dead-letter";
        var deadQueue = $"{options.QueueName}.dead-letter";
        await channel.ExchangeDeclareAsync(messaging.ExchangeName, ExchangeType.Topic,
            durable: true, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(deadExchange, ExchangeType.Fanout,
            durable: true, autoDelete: false, arguments: null, cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(deadQueue, durable: true, exclusive: false,
            autoDelete: false, arguments: null, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(deadQueue, deadExchange, string.Empty,
            arguments: null, cancellationToken: cancellationToken);
        var queueArguments = new Dictionary<string, object?>
        { ["x-dead-letter-exchange"] = deadExchange };
        await channel.QueueDeclareAsync(options.QueueName, durable: true, exclusive: false,
            autoDelete: false, arguments: queueArguments, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(options.QueueName, messaging.ExchangeName,
            "ordering.order-placed", arguments: null, cancellationToken: cancellationToken);
        await channel.QueueBindAsync(options.QueueName, messaging.ExchangeName,
            "ordering.order-status-changed", arguments: null, cancellationToken: cancellationToken);
        await channel.BasicQosAsync(0, 1, global: false, cancellationToken);

        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => HandleAsync(channel, delivery, cancellationToken);
        consumer.ShutdownAsync += (_, _) => { stopped.TrySetResult(); return Task.CompletedTask; };
        await channel.BasicConsumeAsync(options.QueueName, autoAck: false, consumer,
            cancellationToken: cancellationToken);
        await stopped.Task.WaitAsync(cancellationToken);
        throw new InvalidOperationException("The RabbitMQ notification consumer was disconnected.");
    }

    private async Task HandleAsync(IChannel channel, BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        IntegrationEventEnvelope envelope;
        try
        {
            envelope = ReadEnvelope(delivery);
        }
        catch (Exception exception)
        {
            LogRejectedMessage(logger, delivery.RoutingKey, exception);
            await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false, cancellationToken);
            return;
        }

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetServices<IIntegrationEventConsumer>()
                .SingleOrDefault(value => value.EventName == envelope.Name &&
                    value.EventVersion == envelope.Version);
            if (handler is null)
            {
                LogRejectedMessage(logger, delivery.RoutingKey,
                    new InvalidOperationException("No notification consumer supports the event contract."));
                await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false,
                    cancellationToken);
                return;
            }
            var outcome = await scope.ServiceProvider.GetRequiredService<InboxProcessor>()
                .ProcessAsync(envelope, handler, cancellationToken);
            if (outcome == InboxProcessingOutcome.RetryScheduled)
                await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false,
                    requeue: true, cancellationToken);
            else if (outcome == InboxProcessingOutcome.DeadLettered)
                await channel.BasicRejectAsync(delivery.DeliveryTag, requeue: false,
                    cancellationToken);
            else
                await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            LogConsumerFailure(logger, exception);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false,
                requeue: true, cancellationToken);
        }
    }

    private static IntegrationEventEnvelope ReadEnvelope(BasicDeliverEventArgs delivery)
    {
        if (!Guid.TryParse(delivery.BasicProperties.MessageId, out var id) ||
            string.IsNullOrWhiteSpace(delivery.BasicProperties.Type) ||
            !TryReadInt32(delivery.BasicProperties.Headers, "event-version", out var version) ||
            !TryReadGuid(delivery.BasicProperties.Headers, "aggregate-id", out var aggregateId) ||
            !TryReadInt64(delivery.BasicProperties.Headers, "aggregate-version", out var aggregateVersion))
            throw new InvalidOperationException("The integration-event envelope is invalid.");
        return new IntegrationEventEnvelope(id, delivery.BasicProperties.Type, version,
            aggregateId, aggregateVersion,
            DateTimeOffset.FromUnixTimeSeconds(delivery.BasicProperties.Timestamp.UnixTime),
            Encoding.UTF8.GetString(delivery.Body.Span));
    }

    private static bool TryReadGuid(IDictionary<string, object?>? headers, string name,
        out Guid value) => Guid.TryParse(ReadString(headers, name), out value);

    private static bool TryReadInt32(IDictionary<string, object?>? headers, string name,
        out int value) => int.TryParse(ReadString(headers, name), out value);

    private static bool TryReadInt64(IDictionary<string, object?>? headers, string name,
        out long value) => long.TryParse(ReadString(headers, name), out value);

    private static string? ReadString(IDictionary<string, object?>? headers, string name)
    {
        if (headers is null || !headers.TryGetValue(name, out var value)) return null;
        return value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> bytes => Encoding.UTF8.GetString(bytes.Span),
            _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
        };
    }
}
