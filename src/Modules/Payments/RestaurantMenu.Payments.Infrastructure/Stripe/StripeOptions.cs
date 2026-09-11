namespace RestaurantMenu.Payments.Infrastructure.Stripe;
public sealed record StripeOptions(string SecretKey,string WebhookSecret,Uri ApiBaseUri,TimeSpan Timeout);
