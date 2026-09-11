using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Payments.Domain.Payments;

public sealed class PaymentEvent : Entity<Guid>
{
    private PaymentEvent() : base(Guid.Empty)
    {
        EventType = string.Empty;
    }

    internal PaymentEvent(Guid id, PaymentId paymentId, string eventType,
        string? providerEventId, long? amountMinor, long? platformFeeAmountMinor,
        DateTimeOffset occurredAtUtc) : base(id)
    {
        PaymentId = paymentId;
        EventType = eventType;
        ProviderEventId = providerEventId;
        AmountMinor = amountMinor;
        PlatformFeeAmountMinor = platformFeeAmountMinor;
        OccurredAtUtc = occurredAtUtc;
    }

    public PaymentId PaymentId { get; }
    public string EventType { get; }
    public string? ProviderEventId { get; }
    public long? AmountMinor { get; }
    public long? PlatformFeeAmountMinor { get; }
    public DateTimeOffset OccurredAtUtc { get; }
}
