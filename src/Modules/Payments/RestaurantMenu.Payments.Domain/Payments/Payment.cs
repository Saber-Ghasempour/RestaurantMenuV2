using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Payments.Domain.Payments;

public sealed class Payment : AggregateRoot<PaymentId>
{
    public const int MaxProviderReferenceLength = 255;
    private readonly List<PaymentEvent> _events = [];

    private Payment() : base(default)
    {
        ConnectedAccountId = string.Empty;
        Currency = string.Empty;
    }

    private Payment(PaymentId id, Guid restaurantId, Guid branchId, Guid orderId,
        Guid diningSessionId, string connectedAccountId, BillAmount bill,
        long grossAmountMinor, int commissionRateBasisPoints,
        long platformFeeAmountMinor, DateTimeOffset createdAtUtc) : base(id)
    {
        RestaurantId = restaurantId;
        BranchId = branchId;
        OrderId = orderId;
        DiningSessionId = diningSessionId;
        ConnectedAccountId = connectedAccountId;
        SubtotalAmountMinor = bill.SubtotalAmountMinor;
        DiscountAmountMinor = bill.DiscountAmountMinor;
        TaxAmountMinor = bill.TaxAmountMinor;
        TipAmountMinor = bill.TipAmountMinor;
        OtherFeeAmountMinor = bill.OtherFeeAmountMinor;
        GrossAmountMinor = grossAmountMinor;
        Currency = bill.Currency.Trim().ToUpperInvariant();
        CommissionRateBasisPoints = commissionRateBasisPoints;
        PlatformFeeAmountMinor = platformFeeAmountMinor;
        RestaurantProceedsAmountMinor = grossAmountMinor - platformFeeAmountMinor;
        CreatedAtUtc = createdAtUtc;
        _events.Add(NewEvent("Created", null, grossAmountMinor,
            platformFeeAmountMinor, createdAtUtc));
    }

    public Guid RestaurantId { get; }
    public Guid BranchId { get; }
    public Guid OrderId { get; }
    public Guid DiningSessionId { get; }
    public string ConnectedAccountId { get; }
    public long SubtotalAmountMinor { get; }
    public long DiscountAmountMinor { get; }
    public long TaxAmountMinor { get; }
    public long TipAmountMinor { get; }
    public long OtherFeeAmountMinor { get; }
    public long GrossAmountMinor { get; }
    public string Currency { get; }
    public int CommissionRateBasisPoints { get; }
    public long PlatformFeeAmountMinor { get; }
    public long RestaurantProceedsAmountMinor { get; }
    public string? ProviderPaymentIntentId { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public long RefundedAmountMinor { get; private set; }
    public long RefundedPlatformFeeAmountMinor { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? SucceededAtUtc { get; private set; }
    public long Version { get; private set; } = 1;
    public IReadOnlyCollection<PaymentEvent> Events => _events.AsReadOnly();

    public static Result<Payment> Create(PaymentId id, Guid restaurantId,
        Guid branchId, Guid orderId, Guid diningSessionId, string? connectedAccountId,
        BillAmount? bill, int commissionRateBasisPoints, DateTimeOffset createdAtUtc)
    {
        if (id == default || restaurantId == Guid.Empty || branchId == Guid.Empty ||
            orderId == Guid.Empty || diningSessionId == Guid.Empty)
            return Result.Failure<Payment>(PaymentErrors.InvalidScope);

        connectedAccountId = connectedAccountId?.Trim();
        if (string.IsNullOrWhiteSpace(connectedAccountId) ||
            connectedAccountId.Length > MaxProviderReferenceLength)
            return Result.Failure<Payment>(PaymentErrors.InvalidConnectedAccount);

        if (commissionRateBasisPoints is < 1 or > 10_000)
            return Result.Failure<Payment>(PaymentErrors.InvalidCommissionRate);

        if (bill is null || bill.SubtotalAmountMinor < 0 || bill.DiscountAmountMinor < 0 ||
            bill.TaxAmountMinor < 0 || bill.TipAmountMinor < 0 || bill.OtherFeeAmountMinor < 0)
            return Result.Failure<Payment>(PaymentErrors.InvalidBillAmount);

        var currency = bill.Currency?.Trim().ToUpperInvariant();
        if (currency is null || currency.Length != 3 || !currency.All(char.IsAsciiLetter))
            return Result.Failure<Payment>(PaymentErrors.InvalidCurrency);

        long gross;
        try
        {
            gross = checked(bill.SubtotalAmountMinor - bill.DiscountAmountMinor +
                bill.TaxAmountMinor + bill.TipAmountMinor + bill.OtherFeeAmountMinor);
        }
        catch (OverflowException)
        {
            return Result.Failure<Payment>(PaymentErrors.InvalidBillAmount);
        }

        if (gross <= 0)
            return Result.Failure<Payment>(PaymentErrors.InvalidBillAmount);

        var fee = CalculateProportionalAmount(gross, commissionRateBasisPoints);
        var normalizedBill = bill with { Currency = currency };
        return Result.Success(new Payment(id, restaurantId, branchId, orderId,
            diningSessionId, connectedAccountId, normalizedBill, gross,
            commissionRateBasisPoints, fee, createdAtUtc));
    }

    public Result<Payment> AttachProviderIntent(string? paymentIntentId,
        string? providerEventId, DateTimeOffset occurredAtUtc)
    {
        paymentIntentId = NormalizeProviderReference(paymentIntentId);
        providerEventId = NormalizeProviderReference(providerEventId);
        if (paymentIntentId is null || providerEventId is null)
            return Result.Failure<Payment>(PaymentErrors.InvalidProviderReference);
        if (ProviderPaymentIntentId is not null)
            return Result.Failure<Payment>(PaymentErrors.ProviderIntentAlreadyAttached);

        ProviderPaymentIntentId = paymentIntentId;
        Status = PaymentStatus.Processing;
        Version++;
        _events.Add(NewEvent("ProviderIntentAttached", providerEventId, null, null, occurredAtUtc));
        return Result.Success(this);
    }

    public Result<Payment> MarkSucceeded(string? providerEventId,
        DateTimeOffset occurredAtUtc)
    {
        providerEventId = NormalizeProviderReference(providerEventId);
        if (providerEventId is null)
            return Result.Failure<Payment>(PaymentErrors.InvalidProviderReference);
        if (HasProviderEvent(providerEventId))
            return Result.Success(this);
        if (Status != PaymentStatus.Processing)
            return Result.Failure<Payment>(PaymentErrors.InvalidTransition);

        Status = PaymentStatus.Succeeded;
        SucceededAtUtc = occurredAtUtc;
        Version++;
        _events.Add(NewEvent("Succeeded", providerEventId, GrossAmountMinor,
            PlatformFeeAmountMinor, occurredAtUtc));
        return Result.Success(this);
    }

    public Result<Payment> RecordRefund(string? providerEventId, long amountMinor,
        DateTimeOffset occurredAtUtc)
    {
        providerEventId = NormalizeProviderReference(providerEventId);
        if (providerEventId is null)
            return Result.Failure<Payment>(PaymentErrors.InvalidProviderReference);
        if (HasProviderEvent(providerEventId))
            return Result.Success(this);
        if (Status is not (PaymentStatus.Succeeded or PaymentStatus.PartiallyRefunded) ||
            amountMinor <= 0 || amountMinor > GrossAmountMinor - RefundedAmountMinor)
            return Result.Failure<Payment>(PaymentErrors.InvalidRefund);

        var newRefundedAmount = checked(RefundedAmountMinor + amountMinor);
        var newRefundedFee = CalculateProportionalAmount(
            newRefundedAmount, CommissionRateBasisPoints);
        var feeForEvent = newRefundedFee - RefundedPlatformFeeAmountMinor;
        RefundedAmountMinor = newRefundedAmount;
        RefundedPlatformFeeAmountMinor = newRefundedFee;
        Status = RefundedAmountMinor == GrossAmountMinor
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;
        Version++;
        _events.Add(NewEvent("Refunded", providerEventId, amountMinor,
            feeForEvent, occurredAtUtc));
        return Result.Success(this);
    }

    private bool HasProviderEvent(string providerEventId) =>
        _events.Any(paymentEvent => paymentEvent.ProviderEventId == providerEventId);

    private PaymentEvent NewEvent(string eventType, string? providerEventId,
        long? amountMinor, long? platformFeeAmountMinor, DateTimeOffset occurredAtUtc) =>
        new(Guid.CreateVersion7(), Id, eventType, providerEventId, amountMinor,
            platformFeeAmountMinor, occurredAtUtc);

    private static long CalculateProportionalAmount(long amountMinor, int basisPoints)
    {
        var whole = checked((amountMinor / 10_000) * basisPoints);
        var remainder = amountMinor % 10_000;
        var roundedRemainder = (remainder * basisPoints + 5_000) / 10_000;
        return checked(whole + roundedRemainder);
    }

    private static string? NormalizeProviderReference(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > MaxProviderReferenceLength
            ? null
            : value;
    }
}
