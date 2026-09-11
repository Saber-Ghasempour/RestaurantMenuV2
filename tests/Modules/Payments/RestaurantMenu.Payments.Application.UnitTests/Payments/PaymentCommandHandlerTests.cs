using RestaurantMenu.Payments.Application.Abstractions;
using RestaurantMenu.Payments.Application.Payments;
using RestaurantMenu.Payments.Domain.Payments;
using RestaurantMenu.Payments.Domain.Profiles;

namespace RestaurantMenu.Payments.Application.UnitTests.Payments;

public sealed class PaymentCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreatePaymentChargesGrossBillAndThreePercentCommission()
    {
        var restaurantId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var profile = RestaurantPaymentProfile.Create(restaurantId, "acct_restaurant", "PT", "EUR", 300, Now).Value;
        profile.Enable();
        var provider = new FakePaymentProvider();
        var repository = new FakeRepository(profile);
        var orders = new FakeOrderProvider(new PayableOrderSnapshot(
            orderId, restaurantId, Guid.NewGuid(), Guid.NewGuid(), 10_000, 500, 1_000, 200, "EUR"));
        var handler = new CreatePaymentCommandHandler(orders, repository, repository, provider,
            repository, new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreatePaymentCommand("capability", orderId, 500), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(11_200, result.Value.GrossAmountMinor);
        Assert.Equal(336, result.Value.PlatformFeeAmountMinor);
        Assert.Equal(("acct_restaurant", 11_200L, "EUR", 336L), provider.LastIntent);
        Assert.NotNull(repository.Payment);
    }

    [Fact]
    public async Task ProcessWebhookHandlesStripeRefundUpdatedAndIsIdempotent()
    {
        var payment = Payment.Create(PaymentId.New(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "acct_restaurant", new BillAmount(10_000, 0, 0, 0, 0, "EUR"), 300, Now).Value;
        payment.AttachProviderIntent("pi_123", "created:pi_123", Now);
        payment.MarkSucceeded("evt_succeeded", Now);
        var repository = new FakeRepository(payment);
        var provider = new FakePaymentProvider
        {
            WebhookEvent = new VerifiedPaymentEvent("evt_refund", "pi_123", "refund.updated", 1_000, Now)
        };
        var handler = new ProcessWebhookCommandHandler(provider, repository, repository,
            new FixedTimeProvider(Now));

        var first = await handler.Handle(new ProcessWebhookCommand("payload", "signature"), default);
        var replay = await handler.Handle(new ProcessWebhookCommand("payload", "signature"), default);

        Assert.True(first.IsSuccess);
        Assert.True(replay.IsSuccess);
        Assert.Equal(PaymentStatus.PartiallyRefunded, payment.Status);
        Assert.Equal(1_000, payment.RefundedAmountMinor);
        Assert.Equal(30, payment.RefundedPlatformFeeAmountMinor);
    }

    [Fact]
    public async Task RefundRequestsApplicationFeeReversalThroughProvider()
    {
        var restaurantId = Guid.NewGuid();
        var payment = Payment.Create(PaymentId.New(), restaurantId, Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "acct_restaurant", new BillAmount(10_000, 0, 0, 0, 0, "EUR"), 300, Now).Value;
        payment.AttachProviderIntent("pi_123", "created:pi_123", Now);
        payment.MarkSucceeded("evt_succeeded", Now);
        var repository = new FakeRepository(payment);
        var provider = new FakePaymentProvider();
        var handler = new RefundPaymentCommandHandler(repository, provider);

        var result = await handler.Handle(new RefundPaymentCommand(
            restaurantId, payment.Id.Value, 1_000), default);

        Assert.True(result.IsSuccess);
        Assert.Equal("re_123", result.Value);
        Assert.Equal(("acct_restaurant", "pi_123", 1_000L), provider.LastRefund);
    }

    [Fact]
    public async Task OnboardingRejectsInsecureRedirectBeforeCallingProvider()
    {
        var repository = new FakeRepository();
        var provider = new FakePaymentProvider();
        var handler = new StartOnboardingCommandHandler(repository,
            provider, repository, new FixedTimeProvider(Now));

        var result = await handler.Handle(new StartOnboardingCommand(Guid.NewGuid(), "PT", "EUR",
            new Uri("http://restaurant.example/refresh"),
            new Uri("https://restaurant.example/return")), default);

        Assert.True(result.IsFailure);
        Assert.Equal("Payments.InvalidProfile", result.Error.Code);
        Assert.Equal(0, provider.CreateAccountCalls);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeOrderProvider(PayableOrderSnapshot snapshot) : IPayableOrderProvider
    {
        public Task<PayableOrderSnapshot?> GetAsync(string diningSessionToken, Guid orderId,
            long tipAmountMinor, CancellationToken cancellationToken) => Task.FromResult<PayableOrderSnapshot?>(snapshot);
    }

    private sealed class FakePaymentProvider : IPaymentProvider
    {
        public (string Account, long Amount, string Currency, long Fee)? LastIntent { get; private set; }
        public (string Account, string Intent, long Amount)? LastRefund { get; private set; }
        public int CreateAccountCalls { get; private set; }
        public VerifiedPaymentEvent WebhookEvent { get; init; } = new("evt", "pi", "ignored", null, Now);
        public Task<ProviderAccount> CreateConnectedAccountAsync(string country, string currency, string idempotencyKey, CancellationToken cancellationToken)
        {
            CreateAccountCalls++;
            throw new NotSupportedException();
        }
        public Task<ProviderOnboardingLink> CreateOnboardingLinkAsync(string accountId, Uri refreshUrl, Uri returnUrl, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> IsAccountReadyAsync(string accountId, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<ProviderPaymentIntent> CreatePaymentIntentAsync(string accountId, long amountMinor,
            string currency, long applicationFeeMinor, string idempotencyKey, CancellationToken cancellationToken)
        {
            LastIntent = (accountId, amountMinor, currency, applicationFeeMinor);
            return Task.FromResult(new ProviderPaymentIntent("pi_123", "client_secret"));
        }
        public Task<ProviderRefund> CreateRefundAsync(string accountId, string paymentIntentId,
            long amountMinor, string idempotencyKey, CancellationToken cancellationToken)
        {
            LastRefund = (accountId, paymentIntentId, amountMinor);
            return Task.FromResult(new ProviderRefund("re_123"));
        }
        public VerifiedPaymentEvent VerifyWebhook(string payload, string signatureHeader, DateTimeOffset now) => WebhookEvent;
    }

    private sealed class FakeRepository : IPaymentRepository, IPaymentProfileRepository, IPaymentsUnitOfWork
    {
        private readonly RestaurantPaymentProfile? _profile;
        public FakeRepository() { }
        public FakeRepository(RestaurantPaymentProfile profile) => _profile = profile;
        public FakeRepository(Payment payment) => Payment = payment;
        public Payment? Payment { get; private set; }
        public void Add(Payment payment) => Payment = payment;
        public Task<Payment?> GetAsync(PaymentId paymentId, Guid restaurantId,
            CancellationToken cancellationToken) => Task.FromResult(Payment);
        void IPaymentProfileRepository.Add(RestaurantPaymentProfile profile) => throw new NotSupportedException();
        public Task<Payment?> GetByOrderAsync(Guid orderId, CancellationToken cancellationToken) => Task.FromResult(Payment);
        public Task<Payment?> GetByProviderIntentAsync(string providerIntentId, CancellationToken cancellationToken) => Task.FromResult(Payment);
        public Task<RestaurantPaymentProfile?> GetAsync(Guid restaurantId, CancellationToken cancellationToken) => Task.FromResult(_profile);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
