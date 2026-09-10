using System.Diagnostics.Metrics;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public static class MessagingTelemetry
{
    public const string MeterName = "RestaurantMenu.Ordering.Messaging";
    private static readonly Meter Meter = new(MeterName);
    internal static readonly Counter<long> OutboxPublished = Meter.CreateCounter<long>("messaging.outbox.published");
    internal static readonly Counter<long> OutboxFailures = Meter.CreateCounter<long>("messaging.outbox.failures");
    internal static readonly Counter<long> OutboxDeadLetters = Meter.CreateCounter<long>("messaging.outbox.dead_letters");
    internal static readonly Counter<long> InboxProcessed = Meter.CreateCounter<long>("messaging.inbox.processed");
    internal static readonly Counter<long> InboxDuplicates = Meter.CreateCounter<long>("messaging.inbox.duplicates");
    internal static readonly Counter<long> InboxFailures = Meter.CreateCounter<long>("messaging.inbox.failures");
    internal static readonly Counter<long> InboxDeadLetters = Meter.CreateCounter<long>("messaging.inbox.dead_letters");
}
