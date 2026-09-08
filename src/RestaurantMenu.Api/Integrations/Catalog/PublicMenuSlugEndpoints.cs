using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Api.Integrations.Catalog;

public static class PublicMenuSlugEndpoints
{
    public static IEndpointRouteBuilder MapPublicMenuSlugEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/public/restaurants/by-slug/{slug}/menu", GetAsync)
            .WithName("GetPublicMenuBySlug")
            .WithTags("Public Menu")
            .Produces<PublicMenuResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        string slug,
        IRestaurantSlugLookup lookup,
        IQueryHandler<GetPublicMenuQuery, Result<PublicMenuResponse>> handler,
        CancellationToken cancellationToken)
    {
        var restaurantId = await lookup.FindRestaurantIdAsync(slug, cancellationToken);
        if (restaurantId is null)
        {
            return Results.Problem(statusCode: StatusCodes.Status404NotFound,
                title: "Restaurants.NotFound", detail: "Restaurant was not found.");
        }

        var result = await handler.Handle(new GetPublicMenuQuery(restaurantId.Value), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.Error.ToProblem();
    }
}
