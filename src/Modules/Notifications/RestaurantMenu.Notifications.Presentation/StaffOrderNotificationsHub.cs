using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Presentation.Abstractions.Authorization;

namespace RestaurantMenu.Notifications.Presentation;

[Authorize(Policy = Permissions.OrdersRead)]
public sealed class StaffOrderNotificationsHub(IOrderStaffAccessProvider staffAccess)
    : Hub<IOrderNotificationClient>
{
    public const string Path = "/hubs/staff-order-notifications";

    public async Task JoinBranch(Guid restaurantId, Guid branchId)
    {
        var subject = Context.User?.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(subject))
            throw new HubException("The Branch subscription is not authorized.");
        var role = await staffAccess.GetActiveRoleAsync(restaurantId, branchId, subject,
            Context.ConnectionAborted);
        if (role is null) throw new HubException("The Branch subscription is not authorized.");
        await Groups.AddToGroupAsync(Context.ConnectionId,
            OrderNotificationGroups.StaffBranch(restaurantId, branchId),
            Context.ConnectionAborted);
    }
}
