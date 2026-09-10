using RestaurantMenu.Ordering.Application.Abstractions;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    private OutboxMessage() { Name = string.Empty; Payload = string.Empty; }

    internal OutboxMessage(IntegrationEventEnvelope envelope)
    {
        Id = envelope.Id; Name = envelope.Name; EventVersion = envelope.Version;
        AggregateId = envelope.AggregateId; AggregateVersion = envelope.AggregateVersion;
        OccurredAtUtc = envelope.OccurredAtUtc; Payload = envelope.Payload;
        NextAttemptAtUtc = envelope.OccurredAtUtc;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public int EventVersion { get; private set; }
    public Guid AggregateId { get; private set; }
    public long AggregateVersion { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string Payload { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset NextAttemptAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }
    public DateTimeOffset? DeadLetteredAtUtc { get; private set; }
    public string? LastError { get; private set; }
    public Guid? LockId { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }

    public IntegrationEventEnvelope ToEnvelope() => new(Id, Name, EventVersion,
        AggregateId, AggregateVersion, OccurredAtUtc, Payload);

    internal void Claim(Guid lockId, DateTimeOffset lockedUntilUtc)
    { LockId = lockId; LockedUntilUtc = lockedUntilUtc; }

    internal void MarkProcessed(DateTimeOffset now)
    { ProcessedAtUtc = now; LastError = null; LockId = null; LockedUntilUtc = null; }

    internal void MarkFailed(string error, DateTimeOffset now, int maximumAttempts,
        TimeSpan delay)
    {
        AttemptCount++;
        LastError = error.Length <= 2000 ? error : error[..2000];
        ProcessedAtUtc = null;
        LockId = null; LockedUntilUtc = null;
        if (AttemptCount >= maximumAttempts) DeadLetteredAtUtc = now;
        else NextAttemptAtUtc = now.Add(delay);
    }
}
