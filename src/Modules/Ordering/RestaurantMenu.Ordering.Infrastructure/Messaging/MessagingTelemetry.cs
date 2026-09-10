using System.Diagnostics;
using System.Diagnostics.Metrics;
using RestaurantMenu.Ordering.Application.Abstractions;

namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public static class MessagingTelemetry
{
    public const string MeterName = "RestaurantMenu.Ordering.Messaging";
    public const string ActivitySourceName = "RestaurantMenu.Ordering.Messaging";
    private static readonly Meter Meter = new(MeterName);
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Counter<long> OutboxPublished = Meter.CreateCounter<long>("messaging.outbox.published");
    private static readonly Counter<long> OutboxFailures = Meter.CreateCounter<long>("messaging.outbox.failures");
    private static readonly Counter<long> OutboxDeadLetters = Meter.CreateCounter<long>("messaging.outbox.dead_letters");
    private static readonly Counter<long> InboxProcessed = Meter.CreateCounter<long>("messaging.inbox.processed");
    private static readonly Counter<long> InboxDuplicates = Meter.CreateCounter<long>("messaging.inbox.duplicates");
    private static readonly Counter<long> InboxFailures = Meter.CreateCounter<long>("messaging.inbox.failures");
    private static readonly Counter<long> InboxDeadLetters = Meter.CreateCounter<long>("messaging.inbox.dead_letters");
    private static long _oldestPendingAgeSeconds;
    private static long _outboxDeadLetterCount;
    private static long _inboxDeadLetterCount;

    static MessagingTelemetry()
    {
        Meter.CreateObservableGauge("messaging.outbox.oldest_pending_age",
            () => Interlocked.Read(ref _oldestPendingAgeSeconds), unit: "s");
        Meter.CreateObservableGauge("messaging.dead_letters", ObserveDeadLetters);
    }

    public static Activity? StartConsumerActivity(
        IntegrationEventEnvelope envelope,
        string consumerName)
    {
        var parent = default(ActivityContext);
        if (envelope.TraceParent is not null)
            ActivityContext.TryParse(envelope.TraceParent, envelope.TraceState, true, out parent);
        var activity = ActivitySource.StartActivity(
            $"{NormalizeEventName(envelope.Name)} process",
            ActivityKind.Consumer,
            parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.name", NormalizeEventName(envelope.Name));
        activity?.SetTag("messaging.consumer.name", NormalizeConsumer(consumerName));
        activity?.SetTag("messaging.message.id", envelope.Id);
        return activity;
    }

    public static Activity? StartProducerActivity(IntegrationEventEnvelope envelope)
    {
        var parent = default(ActivityContext);
        if (envelope.TraceParent is not null)
            ActivityContext.TryParse(envelope.TraceParent, envelope.TraceState, true, out parent);
        var activity = ActivitySource.StartActivity(
            $"{NormalizeEventName(envelope.Name)} publish",
            ActivityKind.Producer,
            parent);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.operation.name", NormalizeEventName(envelope.Name));
        activity?.SetTag("messaging.message.id", envelope.Id);
        return activity;
    }

    public static void RecordOutboxPublished(string eventName) =>
        OutboxPublished.Add(1, new KeyValuePair<string, object?>("event.name", NormalizeEventName(eventName)));
    public static void RecordOutboxFailure(string eventName) =>
        OutboxFailures.Add(1, new KeyValuePair<string, object?>("event.name", NormalizeEventName(eventName)));
    public static void RecordOutboxDeadLetter(string eventName) =>
        OutboxDeadLetters.Add(1, new KeyValuePair<string, object?>("event.name", NormalizeEventName(eventName)));
    public static void RecordInboxProcessed(string consumer) =>
        InboxProcessed.Add(1, new KeyValuePair<string, object?>("consumer", NormalizeConsumer(consumer)));
    public static void RecordInboxDuplicate(string consumer) =>
        InboxDuplicates.Add(1, new KeyValuePair<string, object?>("consumer", NormalizeConsumer(consumer)));
    public static void RecordInboxFailure(string consumer) =>
        InboxFailures.Add(1, new KeyValuePair<string, object?>("consumer", NormalizeConsumer(consumer)));
    public static void RecordInboxDeadLetter(string consumer) =>
        InboxDeadLetters.Add(1, new KeyValuePair<string, object?>("consumer", NormalizeConsumer(consumer)));

    public static void UpdateBacklog(long oldestPendingAgeSeconds,
        long outboxDeadLetterCount, long inboxDeadLetterCount)
    {
        Interlocked.Exchange(ref _oldestPendingAgeSeconds,
            Math.Max(0, oldestPendingAgeSeconds));
        Interlocked.Exchange(ref _outboxDeadLetterCount,
            Math.Max(0, outboxDeadLetterCount));
        Interlocked.Exchange(ref _inboxDeadLetterCount,
            Math.Max(0, inboxDeadLetterCount));
    }

    private static IEnumerable<Measurement<long>> ObserveDeadLetters()
    {
        yield return new Measurement<long>(Interlocked.Read(ref _outboxDeadLetterCount),
            new KeyValuePair<string, object?>("source", "outbox"));
        yield return new Measurement<long>(Interlocked.Read(ref _inboxDeadLetterCount),
            new KeyValuePair<string, object?>("source", "inbox"));
    }

    private static string NormalizeEventName(string value) => value switch
    {
        "ordering.order-placed" => value,
        "ordering.order-status-changed" => value,
        _ => "other"
    };

    private static string NormalizeConsumer(string value) => value switch
    {
        "notifications.order-placed-v1" => value,
        "notifications.order-status-changed-v1" => value,
        _ => "other"
    };
}
