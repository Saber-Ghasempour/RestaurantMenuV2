using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.ResolvePublicMenuCode;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Api.Integrations.Catalog;

public static class PublicMenuCodeMenuEndpoints
{
    public static IEndpointRouteBuilder MapPublicMenuCodeMenuEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(
                "/api/public/menu-codes/{code}/menu",
                GetAsync)
            .WithName("GetPublicMenuByCode")
            .WithTags("Public Menu")
            .AllowAnonymous()
            .RequireRateLimiting("public-menu-code-resolution");
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        string code,
        IQueryHandler<ResolvePublicMenuCodeQuery,
            Result<ResolvedPublicMenuCode>> resolver,
        IQueryHandler<GetPublicMenuQuery,
            Result<PublicMenuResponse>> restaurantMenuHandler,
        IQueryHandler<GetPublicBranchMenuQuery,
            Result<PublicBranchMenuResponse>> branchMenuHandler,
        CancellationToken cancellationToken)
    {
        var resolution = await resolver.Handle(
            new ResolvePublicMenuCodeQuery(code), cancellationToken);
        if (resolution.IsFailure)
        {
            return resolution.Error.ToProblem();
        }

        if (!resolution.Value.BranchId.HasValue)
        {
            var menu = await restaurantMenuHandler.Handle(
                new GetPublicMenuQuery(resolution.Value.RestaurantId),
                cancellationToken);
            return menu.IsSuccess ? Results.Ok(menu.Value) : menu.Error.ToProblem();
        }

        var branchMenu = await branchMenuHandler.Handle(
            new GetPublicBranchMenuQuery(
                resolution.Value.RestaurantId,
                resolution.Value.BranchId.Value),
            cancellationToken);
        return branchMenu.IsSuccess
            ? Results.Ok(branchMenu.Value)
            : branchMenu.Error.ToProblem();
    }
}
