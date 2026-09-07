using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Presentation.PublicMenus;

public static class PublicMenuEndpoints
{
    public static IEndpointRouteBuilder MapPublicMenuEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet(
                "/api/public/restaurants/{restaurantId:guid}/menu",
                GetPublicMenuAsync)
            .WithName("GetPublicMenu")
            .WithTags("Public Menu")
            .Produces<PublicMenuResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> GetPublicMenuAsync(
        Guid restaurantId,
        IQueryHandler<GetPublicMenuQuery, Result<PublicMenuResponse>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.Handle(
            new GetPublicMenuQuery(restaurantId),
            cancellationToken);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToProblem();
    }
}
