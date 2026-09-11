using System.Net;
using System.Security.Cryptography;
using System.Text;
using RestaurantMenu.Payments.Infrastructure.Stripe;

namespace RestaurantMenu.Payments.Infrastructure.UnitTests.Stripe;

public sealed class StripePaymentProviderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 11, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task PaymentIntentIsDirectChargeWithApplicationFeeAndWalletSupport()
    {
        var handler = new RecordingHandler("{\"id\":\"pi_123\",\"client_secret\":\"secret_123\"}");
        var provider = CreateProvider(handler);

        var result = await provider.CreatePaymentIntentAsync(
            "acct_restaurant", 13_700, "EUR", 411, "order-key", default);

        Assert.Equal("pi_123", result.Id);
        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("https://api.stripe.test/v1/payment_intents", handler.Uri?.ToString());
        Assert.Equal("Bearer sk_test", handler.Authorization);
        Assert.Equal("acct_restaurant", handler.ConnectedAccount);
        Assert.Equal("order-key", handler.IdempotencyKey);
        Assert.Contains("amount=13700", handler.Form, StringComparison.Ordinal);
        Assert.Contains("currency=eur", handler.Form, StringComparison.Ordinal);
        Assert.Contains("application_fee_amount=411", handler.Form, StringComparison.Ordinal);
        Assert.Contains("automatic_payment_methods%5Benabled%5D=true", handler.Form, StringComparison.Ordinal);
    }

    [Fact]
    public void WebhookVerificationRejectsInvalidSignature()
    {
        var provider = CreateProvider(new RecordingHandler("{}"));

        Assert.Throws<CryptographicException>(() =>
            provider.VerifyWebhook("{}", $"t={Now.ToUnixTimeSeconds()},v1=invalid", Now));
    }

    [Fact]
    public async Task RefundReversesApplicationFeeOnConnectedAccount()
    {
        var handler = new RecordingHandler("{\"id\":\"re_123\"}");
        var provider = CreateProvider(handler);

        var result = await provider.CreateRefundAsync(
            "acct_restaurant", "pi_123", 1_000, "refund-key", default);

        Assert.Equal("re_123", result.Id);
        Assert.Equal("acct_restaurant", handler.ConnectedAccount);
        Assert.Equal("refund-key", handler.IdempotencyKey);
        Assert.Contains("payment_intent=pi_123", handler.Form, StringComparison.Ordinal);
        Assert.Contains("amount=1000", handler.Form, StringComparison.Ordinal);
        Assert.Contains("refund_application_fee=true", handler.Form, StringComparison.Ordinal);
    }

    [Fact]
    public void WebhookVerificationAuthenticatesAndParsesRefund()
    {
        const string payload = "{\"id\":\"evt_refund\",\"type\":\"refund.updated\",\"created\":1789128000,\"data\":{\"object\":{\"id\":\"re_123\",\"payment_intent\":\"pi_123\",\"amount\":1000,\"status\":\"succeeded\"}}}";
        var timestamp = Now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var signature = Convert.ToHexStringLower(HMACSHA256.HashData(
            Encoding.UTF8.GetBytes("whsec_test"), Encoding.UTF8.GetBytes(timestamp + "." + payload)));
        var provider = CreateProvider(new RecordingHandler("{}"));

        var result = provider.VerifyWebhook(payload, $"t={timestamp},v1={signature}", Now);

        Assert.Equal("re_123", result.EventId);
        Assert.Equal("pi_123", result.PaymentIntentId);
        Assert.Equal("refund.updated", result.Type);
        Assert.Equal(1_000, result.RefundAmountMinor);
    }

    private static StripePaymentProvider CreateProvider(HttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.stripe.test/") },
            new StripeOptions("sk_test", "whsec_test", new Uri("https://api.stripe.test/"), TimeSpan.FromSeconds(1)));

    private sealed class RecordingHandler(string response) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? Authorization { get; private set; }
        public string? ConnectedAccount { get; private set; }
        public string? IdempotencyKey { get; private set; }
        public string Form { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            Authorization = request.Headers.Authorization?.ToString();
            ConnectedAccount = request.Headers.GetValues("Stripe-Account").Single();
            IdempotencyKey = request.Headers.GetValues("Idempotency-Key").Single();
            Form = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response, Encoding.UTF8, "application/json")
            };
        }
    }
}
