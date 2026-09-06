using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Items.CreateMenuItem;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Presentation.Infrastructure;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Presentation.Items;

public static class MenuItemEndpoints
{
    public static IEndpointRouteBuilder MapMenuItemEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}/items",
                CreateMenuItemAsync)
            .WithName("CreateMenuItem")
            .WithTags("Catalog")
            .Produces<CreateMenuItemResponse>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateMenuItemAsync(
        Guid restaurantId,
        Guid categoryId,
        CreateMenuItemRequest request,
        ICommandHandler<
            CreateMenuItemCommand,
            Result<MenuItemId>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        var command = new CreateMenuItemCommand(
            restaurantId,
            categoryId,
            request.Name,
            request.Description,
            request.PriceAmount,
            request.Currency,
            request.DisplayOrder);
        var result = await handler.Handle(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        var response = new CreateMenuItemResponse(
            result.Value.Value);

        return Results.Created(
            $"/api/restaurants/{restaurantId}/categories/{categoryId}/items/{response.Id}",
            response);
    }
}

public sealed record CreateMenuItemRequest(
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? Currency,
    int DisplayOrder);

public sealed record CreateMenuItemResponse(Guid Id);
