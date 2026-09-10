using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.Orders.GuestOrders;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Notifications.Presentation;

[AllowAnonymous]
public sealed class GuestOrderNotificationsHub(
    IQueryHandler<GetGuestOrderQuery, Result<GuestOrderDetail>> orders)
    : Hub<IOrderNotificationClient>
{
    public const string Path = "/api/public/order-notifications";
    private const string HeaderName = "X-Dining-Session";
    private const string CookieName = "rm_dining_session";

    public async Task<GuestOrderDetail> JoinOrder(Guid orderId)
    {
        var request = Context.GetHttpContext()?.Request;
        var token = request is null ? null : ReadToken(request);
        if (token is null) throw new HubException("The order subscription is not authorized.");
        var result = await orders.Handle(new(token, new OrderId(orderId)), Context.ConnectionAborted);
        if (result.IsFailure) throw new HubException("The order subscription is not authorized.");
        await Groups.AddToGroupAsync(Context.ConnectionId,
            OrderNotificationGroups.GuestOrder(orderId), Context.ConnectionAborted);
        return result.Value;
    }

    private static string? ReadToken(HttpRequest request)
    {
        var hasHeader = request.Headers.TryGetValue(HeaderName, out StringValues values) &&
            values.Count == 1 && !string.IsNullOrWhiteSpace(values[0]);
        var hasCookie = request.Cookies.TryGetValue(CookieName, out var cookie) &&
            !string.IsNullOrWhiteSpace(cookie);
        if (hasHeader && hasCookie &&
            !string.Equals(values[0], cookie, StringComparison.Ordinal)) return null;
        return hasHeader ? values[0] : hasCookie ? cookie : null;
    }
}
