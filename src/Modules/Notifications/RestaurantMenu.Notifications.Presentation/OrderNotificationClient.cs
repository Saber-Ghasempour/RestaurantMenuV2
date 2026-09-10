using RestaurantMenu.Notifications.Application;

namespace RestaurantMenu.Notifications.Presentation;

public interface IOrderNotificationClient
{
    Task OrderUpdated(OrderRealtimeNotification notification);
}
