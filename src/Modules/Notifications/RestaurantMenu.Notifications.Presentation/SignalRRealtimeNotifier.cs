using Microsoft.AspNetCore.SignalR;
using RestaurantMenu.Notifications.Application;

namespace RestaurantMenu.Notifications.Presentation;

public sealed class SignalRRealtimeNotifier(
    IHubContext<GuestOrderNotificationsHub, IOrderNotificationClient> guestHub,
    IHubContext<StaffOrderNotificationsHub, IOrderNotificationClient> staffHub)
    : IRealtimeNotifier
{
    public Task NotifyGuestOrderAsync(OrderRealtimeNotification notification,
        CancellationToken cancellationToken) => guestHub.Clients
        .Group(OrderNotificationGroups.GuestOrder(notification.OrderId))
        .OrderUpdated(notification);

    public Task NotifyStaffBranchAsync(Guid restaurantId, Guid branchId,
        OrderRealtimeNotification notification, CancellationToken cancellationToken) =>
        staffHub.Clients.Group(OrderNotificationGroups.StaffBranch(restaurantId, branchId))
            .OrderUpdated(notification);
}
