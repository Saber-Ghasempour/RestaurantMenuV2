using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using RestaurantMenu.Notifications.Application;
using RestaurantMenu.Presentation.Abstractions.Authorization;

namespace RestaurantMenu.Notifications.Presentation;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsPresentation(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
        return services;
    }

    public static IEndpointRouteBuilder MapNotificationHubs(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHub<GuestOrderNotificationsHub>(GuestOrderNotificationsHub.Path)
            .AllowAnonymous();
        endpoints.MapHub<StaffOrderNotificationsHub>(StaffOrderNotificationsHub.Path)
            .RequireAuthorization(Permissions.OrdersRead);
        return endpoints;
    }
}
