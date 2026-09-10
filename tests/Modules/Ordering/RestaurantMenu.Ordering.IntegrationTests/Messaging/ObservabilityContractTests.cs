using System.Diagnostics;
using System.Diagnostics.Metrics;

using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Infrastructure.Messaging;

namespace RestaurantMenu.Ordering.IntegrationTests.Messaging;

public sealed class ObservabilityContractTests
{
    [Fact]
    public void ConsumerActivityShouldContinuePersistedProducerTrace()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == MessagingTelemetry.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(listener);

        var parent = new ActivityContext(
            ActivityTraceId.CreateFromString("4bf92f3577b34da6a3ce929d0e0e4736"),
            ActivitySpanId.CreateFromString("00f067aa0ba902b7"),
            ActivityTraceFlags.Recorded,
            "vendor=value",
            isRemote: true);
        var envelope = new IntegrationEventEnvelope(
            Guid.NewGuid(),
            "ordering.order-placed",
            1,
            Guid.NewGuid(),
            1,
            DateTimeOffset.UtcNow,
            "{}",
            $"00-{parent.TraceId}-{parent.SpanId}-01",
            parent.TraceState);

        using var activity = MessagingTelemetry.StartConsumerActivity(
            envelope,
            "notifications.order-placed-v1");

        Assert.NotNull(activity);
        Assert.Equal(parent.TraceId, activity.TraceId);
        Assert.Equal(parent.SpanId, activity.ParentSpanId);
        Assert.Equal(ActivityKind.Consumer, activity.Kind);
        Assert.Equal("ordering.order-placed", activity.GetTagItem("messaging.operation.name"));
        Assert.Equal("notifications.order-placed-v1", activity.GetTagItem("messaging.consumer.name"));
    }

    [Fact]
    public void MetricsShouldReplaceUnboundedLabelsAndExposeIdentifierFreeBacklogGauges()
    {
        var measurements = new List<(string Instrument, long Value,
            KeyValuePair<string, object?>[] Tags)>();
        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, meterListener) =>
            {
                if (instrument.Meter.Name == MessagingTelemetry.MeterName)
                    meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
            measurements.Add((instrument.Name, value, tags.ToArray())));
        listener.Start();

        MessagingTelemetry.RecordOutboxPublished("attacker-controlled-event-name");
        MessagingTelemetry.RecordInboxProcessed("attacker-controlled-consumer-name");
        MessagingTelemetry.UpdateBacklog(
            oldestPendingAgeSeconds: 42,
            outboxDeadLetterCount: 3,
            inboxDeadLetterCount: 4);
        listener.RecordObservableInstruments();

        Assert.Contains(measurements, value =>
            value.Instrument == "messaging.outbox.published" &&
            value.Tags.Contains(new("event.name", "other")));
        Assert.Contains(measurements, value =>
            value.Instrument == "messaging.inbox.processed" &&
            value.Tags.Contains(new("consumer", "other")));
        Assert.Contains(measurements, value =>
            value.Instrument == "messaging.outbox.oldest_pending_age" &&
            value.Value == 42 && value.Tags.Length == 0);
        Assert.Contains(measurements, value =>
            value.Instrument == "messaging.dead_letters" &&
            value.Value == 3 && value.Tags.Contains(new("source", "outbox")));
        Assert.DoesNotContain(measurements.SelectMany(value => value.Tags), tag =>
            Equals(tag.Value, "attacker-controlled-event-name") ||
            Equals(tag.Value, "attacker-controlled-consumer-name"));
    }
}
