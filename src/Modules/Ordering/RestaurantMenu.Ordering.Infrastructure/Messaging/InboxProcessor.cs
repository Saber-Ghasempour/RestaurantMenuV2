using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Infrastructure.Database;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public enum InboxProcessingOutcome { Processed, Duplicate, RetryScheduled, DeadLettered }

public sealed class InboxProcessor(OrderingDbContext dbContext, MessagingOptions options,
    TimeProvider timeProvider)
{
    public async Task<InboxProcessingOutcome> ProcessAsync(IntegrationEventEnvelope envelope,
        IIntegrationEventConsumer handler, CancellationToken cancellationToken)
    {
        if (!string.Equals(envelope.Name, handler.EventName, StringComparison.Ordinal) ||
            envelope.Version != handler.EventVersion)
            throw new InvalidOperationException("The integration-event contract does not match the handler.");

        var key = $"{envelope.Id:D}:{handler.ConsumerName}";
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(key, cancellationToken);
        var message = await dbContext.InboxMessages.SingleOrDefaultAsync(value =>
            value.MessageId == envelope.Id && value.Consumer == handler.ConsumerName,
            cancellationToken);
        if (message?.ProcessedAtUtc is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            MessagingTelemetry.InboxDuplicates.Add(1,
                new KeyValuePair<string, object?>("consumer", handler.ConsumerName));
            return InboxProcessingOutcome.Duplicate;
        }
        if (message?.DeadLetteredAtUtc is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return InboxProcessingOutcome.DeadLettered;
        }

        try
        {
            message ??= new InboxMessage(envelope.Id, handler.ConsumerName,
                envelope.Name, timeProvider.GetUtcNow());
            if (dbContext.Entry(message).State == EntityState.Detached)
                dbContext.InboxMessages.Add(message);
            await handler.HandleAsync(envelope, cancellationToken);
            message.MarkProcessed(timeProvider.GetUtcNow());
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            MessagingTelemetry.InboxProcessed.Add(1,
                new KeyValuePair<string, object?>("consumer", handler.ConsumerName));
            return InboxProcessingOutcome.Processed;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();
            return await RecordFailureAsync(envelope, handler.ConsumerName,
                exception.Message, key, cancellationToken);
        }
    }

    private async Task<InboxProcessingOutcome> RecordFailureAsync(IntegrationEventEnvelope envelope,
        string consumer, string error, string lockKey, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await LockAsync(lockKey, cancellationToken);
        var message = await dbContext.InboxMessages.SingleOrDefaultAsync(value =>
            value.MessageId == envelope.Id && value.Consumer == consumer, cancellationToken);
        message ??= new InboxMessage(envelope.Id, consumer, envelope.Name, timeProvider.GetUtcNow());
        if (dbContext.Entry(message).State == EntityState.Detached) dbContext.InboxMessages.Add(message);
        message.MarkFailed(error, timeProvider.GetUtcNow(), options.MaximumAttempts);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        MessagingTelemetry.InboxFailures.Add(1,
            new KeyValuePair<string, object?>("consumer", consumer));
        if (message.DeadLetteredAtUtc is not null)
            MessagingTelemetry.InboxDeadLetters.Add(1,
                new KeyValuePair<string, object?>("consumer", consumer));
        return message.DeadLetteredAtUtc is null
            ? InboxProcessingOutcome.RetryScheduled
            : InboxProcessingOutcome.DeadLettered;
    }

    private Task<int> LockAsync(string key, CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
}
