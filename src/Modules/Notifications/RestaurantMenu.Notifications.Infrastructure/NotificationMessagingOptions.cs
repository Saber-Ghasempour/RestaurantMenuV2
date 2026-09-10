namespace RestaurantMenu.Notifications.Infrastructure;

public sealed record NotificationMessagingOptions(string QueueName,
    TimeSpan RecoveryDelay)
{
    public const string DefaultQueueName = "restaurant-menu.notifications";
}
