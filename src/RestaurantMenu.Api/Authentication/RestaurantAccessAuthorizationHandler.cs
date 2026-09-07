using Microsoft.AspNetCore.Authorization;

using RestaurantMenu.Application.Abstractions.Security;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Api.Authentication;

public sealed class RestaurantAccessAuthorizationHandler(
    ICurrentUser currentUser,
    IRestaurantMembershipReadService membershipReadService)
    : AuthorizationHandler<RestaurantAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RestaurantAccessRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.Identity?.IsAuthenticated != true ||
            !context.User.HasClaim(claim => claim.Type == "sub") ||
            context.Resource is not HttpContext httpContext ||
            !Guid.TryParse(
                httpContext.Request.RouteValues["restaurantId"]?.ToString(),
                out var restaurantId))
        {
            return;
        }

        var hasAccess =
            await membershipReadService.HasAccessAsync(
                new RestaurantId(restaurantId),
                currentUser.Subject,
                httpContext.RequestAborted);

        if (hasAccess)
        {
            context.Succeed(requirement);
        }
    }
}
