using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace RestaurantMenu.Presentation.Abstractions.Authorization;

public static class AuthorizationEndpointConventionBuilderExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permission)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return builder
            .RequireAuthorization(permission)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    public static RouteHandlerBuilder RequireRestaurantAccess(
        this RouteHandlerBuilder builder,
        string permission)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permission);

        return builder
            .RequireAuthorization(
                permission,
                RestaurantAccessPolicy.Name)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }
}
