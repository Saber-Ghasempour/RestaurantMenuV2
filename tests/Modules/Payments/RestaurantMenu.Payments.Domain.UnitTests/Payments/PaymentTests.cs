using RestaurantMenu.Payments.Domain.Payments;

namespace RestaurantMenu.Payments.Domain.UnitTests.Payments;

public sealed class PaymentTests
{
    private static readonly Guid RestaurantId = Guid.CreateVersion7();
    private static readonly Guid BranchId = Guid.CreateVersion7();
    private static readonly Guid OrderId = Guid.CreateVersion7();
    private static readonly Guid SessionId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateCalculatesThreePercentFromCompleteGrossBill()
    {
        var result = Payment.Create(PaymentId.New(), RestaurantId, BranchId, OrderId,
            SessionId, "acct_restaurant", new BillAmount(10_000, 500, 2_300, 1_200, 700, "EUR"),
            commissionRateBasisPoints: 300, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(13_700, result.Value.GrossAmountMinor);
        Assert.Equal(411, result.Value.PlatformFeeAmountMinor);
        Assert.Equal(13_289, result.Value.RestaurantProceedsAmountMinor);
        Assert.Equal(300, result.Value.CommissionRateBasisPoints);
    }

    [Fact]
    public void CreateRoundsCommissionToNearestMinorUnitAwayFromZero()
    {
        var result = Payment.Create(PaymentId.New(), RestaurantId, BranchId, OrderId,
            SessionId, "acct_restaurant", new BillAmount(1_050, 0, 0, 0, 0, "EUR"), 300, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(32, result.Value.PlatformFeeAmountMinor);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10_001)]
    public void CreateRejectsInvalidCommissionRate(int basisPoints)
    {
        var result = Payment.Create(PaymentId.New(), RestaurantId, BranchId, OrderId,
            SessionId, "acct_restaurant", new BillAmount(1_000, 0, 0, 0, 0, "EUR"), basisPoints, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("Payments.InvalidCommissionRate", result.Error.Code);
    }

    [Fact]
    public void CreateRejectsDiscountGreaterThanCharges()
    {
        var result = Payment.Create(PaymentId.New(), RestaurantId, BranchId, OrderId,
            SessionId, "acct_restaurant", new BillAmount(100, 101, 0, 0, 0, "EUR"), 300, Now);

        Assert.True(result.IsFailure);
        Assert.Equal("Payments.InvalidBillAmount", result.Error.Code);
    }

    [Fact]
    public void CreateRejectsAmountsThatOverflowCommissionCalculation()
    {
        var result = Payment.Create(PaymentId.New(), RestaurantId,
            BranchId, OrderId, SessionId, "acct_restaurant",
            new BillAmount(long.MaxValue, 0, 0, 0, 0, "EUR"), 300, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(276_701_161_105_643_274, result.Value.PlatformFeeAmountMinor);
    }

    [Fact]
    public void AttachProviderIntentIsOneTimeAndRecordsImmutableEvent()
    {
        var payment = CreatePayment();

        var first = payment.AttachProviderIntent("pi_123", "evt_created", Now.AddSeconds(1));
        var second = payment.AttachProviderIntent("pi_other", "evt_other", Now.AddSeconds(2));

        Assert.True(first.IsSuccess);
        Assert.True(second.IsFailure);
        Assert.Equal("pi_123", payment.ProviderPaymentIntentId);
        Assert.Equal(PaymentStatus.Processing, payment.Status);
        Assert.Equal(2, payment.Events.Count);
    }

    [Fact]
    public void ProviderEventReplayIsAnIdempotentNoOp()
    {
        var payment = CreatePayment();
        payment.AttachProviderIntent("pi_123", "evt_created", Now.AddSeconds(1));
        var first = payment.MarkSucceeded("evt_paid", Now.AddSeconds(2));
        var replay = payment.MarkSucceeded("evt_paid", Now.AddSeconds(3));

        Assert.True(first.IsSuccess);
        Assert.True(replay.IsSuccess);
        Assert.Equal(3, payment.Events.Count);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
    }

    [Fact]
    public void SuccessfulPaymentSupportsProportionalPartialAndFullRefunds()
    {
        var payment = CreatePayment();
        payment.AttachProviderIntent("pi_123", "evt_created", Now.AddSeconds(1));
        payment.MarkSucceeded("evt_paid", Now.AddSeconds(2));

        var partial = payment.RecordRefund("evt_refund_1", 400, Now.AddSeconds(3));
        Assert.True(partial.IsSuccess);
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(12, payment.RefundedPlatformFeeAmountMinor);

        var full = payment.RecordRefund("evt_refund_2", 600, Now.AddSeconds(4));
        Assert.True(full.IsSuccess);
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
        Assert.Equal(30, payment.RefundedPlatformFeeAmountMinor);
    }

    private static Payment CreatePayment() => Payment.Create(PaymentId.New(), RestaurantId,
        BranchId, OrderId, SessionId, "acct_restaurant",
        new BillAmount(1_000, 0, 0, 0, 0, "EUR"), 300, Now).Value;
}
