using Microsoft.Extensions.DependencyInjection;
using RestaurantMenu.Notifications.Application;
using RestaurantMenu.Ordering.Application.Abstractions;

namespace RestaurantMenu.Notifications.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsInfrastructure(this IServiceCollection services,
        NotificationMessagingOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.QueueName) || options.QueueName.Length > 200 ||
            options.RecoveryDelay <= TimeSpan.Zero)
            throw new ArgumentException("Notification messaging options are invalid.", nameof(options));
        services.AddSingleton(options);
        services.AddScoped<IIntegrationEventConsumer, OrderPlacedNotificationConsumer>();
        services.AddScoped<IIntegrationEventConsumer, OrderStatusChangedNotificationConsumer>();
        services.AddHostedService<RabbitMqNotificationConsumerWorker>();
        return services;
    }
}
