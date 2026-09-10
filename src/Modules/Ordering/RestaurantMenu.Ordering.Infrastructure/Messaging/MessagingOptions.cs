namespace RestaurantMenu.Ordering.Infrastructure.Messaging;

public sealed record MessagingOptions(string ConnectionString, string ExchangeName,
    int BatchSize, TimeSpan PollingInterval, TimeSpan InitialRetryDelay,
    int MaximumAttempts, TimeSpan ClaimDuration)
{
    public const string DefaultExchange = "restaurant-menu.events";
}
