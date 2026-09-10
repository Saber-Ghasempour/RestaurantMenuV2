using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Infrastructure.Database;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public sealed class OutboxDispatcher(OrderingDbContext dbContext,
    IIntegrationEventPublisher publisher, MessagingOptions options,
    TimeProvider timeProvider, ILogger<OutboxDispatcher> logger)
{
    private static readonly Action<ILogger, Guid, string, Exception?> LogPublishFailure =
        LoggerMessage.Define<Guid, string>(LogLevel.Warning, new EventId(4101, "OutboxPublishFailed"),
            "Outbox publish failed for {MessageId} ({EventName}).");

    public async Task<int> DispatchBatchAsync(CancellationToken cancellationToken)
    {
        var messages = await ClaimAsync(cancellationToken);
        foreach (var message in messages)
        {
            try
            {
                await publisher.PublishAsync(message.ToEnvelope(), cancellationToken);
                message.MarkProcessed(timeProvider.GetUtcNow());
                await dbContext.SaveChangesAsync(cancellationToken);
                MessagingTelemetry.RecordOutboxPublished(message.Name);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogPublishFailure(logger, message.Id, message.Name, exception);
                var multiplier = 1L << Math.Min(message.AttemptCount, 10);
                var maximumTicks = TimeSpan.FromHours(1).Ticks;
                var initialTicks = Math.Min(options.InitialRetryDelay.Ticks, maximumTicks);
                var delay = TimeSpan.FromTicks(initialTicks > maximumTicks / multiplier
                    ? maximumTicks
                    : initialTicks * multiplier);
                message.MarkFailed(exception.Message, timeProvider.GetUtcNow(),
                    options.MaximumAttempts, delay);
                await dbContext.SaveChangesAsync(cancellationToken);
                MessagingTelemetry.RecordOutboxFailure(message.Name);
                if (message.DeadLetteredAtUtc is not null)
                    MessagingTelemetry.RecordOutboxDeadLetter(message.Name);
            }
        }
        return messages.Count;
    }

    private async Task<IReadOnlyList<OutboxMessage>> ClaimAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var lockId = Guid.NewGuid();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var messages = await dbContext.OutboxMessages.FromSqlInterpolated($$"""
            SELECT * FROM ordering.outbox_messages
            WHERE processed_at_utc IS NULL AND dead_lettered_at_utc IS NULL
              AND next_attempt_at_utc <= {{now}}
              AND (locked_until_utc IS NULL OR locked_until_utc <= {{now}})
            ORDER BY occurred_at_utc, aggregate_version, id
            FOR UPDATE SKIP LOCKED
            LIMIT {{options.BatchSize}}
            """).ToListAsync(cancellationToken);
        foreach (var message in messages) message.Claim(lockId, now.Add(options.ClaimDuration));
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return messages;
    }
}
