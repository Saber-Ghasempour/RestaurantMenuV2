namespace RestaurantMenu.Payments.Application.Abstractions;

public sealed record PayableOrderSnapshot(Guid OrderId,Guid RestaurantId,Guid BranchId,
    Guid DiningSessionId,long SubtotalAmountMinor,long DiscountAmountMinor,
    long TaxAmountMinor,long OtherFeeAmountMinor,string Currency);
public interface IPayableOrderProvider
{
    Task<PayableOrderSnapshot?> GetAsync(string diningSessionToken,Guid orderId,
        long tipAmountMinor,CancellationToken cancellationToken);
}
public sealed record ProviderAccount(string AccountId);
public sealed record ProviderOnboardingLink(Uri Url);
public sealed record ProviderPaymentIntent(string Id,string ClientSecret);
public sealed record ProviderRefund(string Id);
public sealed record VerifiedPaymentEvent(string EventId,string PaymentIntentId,
    string Type,long? RefundAmountMinor,DateTimeOffset OccurredAtUtc);
public interface IPaymentProvider
{
    Task<ProviderAccount> CreateConnectedAccountAsync(string country,string currency,
        string idempotencyKey,CancellationToken cancellationToken);
    Task<ProviderOnboardingLink> CreateOnboardingLinkAsync(string accountId,Uri refreshUrl,
        Uri returnUrl,CancellationToken cancellationToken);
    Task<bool> IsAccountReadyAsync(string accountId,CancellationToken cancellationToken);
    Task<ProviderPaymentIntent> CreatePaymentIntentAsync(string accountId,long amountMinor,
        string currency,long applicationFeeMinor,string idempotencyKey,
        CancellationToken cancellationToken);
    Task<ProviderRefund> CreateRefundAsync(string accountId,string paymentIntentId,
        long amountMinor,string idempotencyKey,CancellationToken cancellationToken);
    VerifiedPaymentEvent VerifyWebhook(string payload,string signatureHeader,DateTimeOffset now);
}
