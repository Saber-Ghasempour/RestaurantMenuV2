using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Items.CreateMenuItem;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Application.Items.ListMenuItems;
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

        endpoints.MapGet(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}/items/{menuItemId:guid}",
                GetMenuItemAsync)
            .WithName("GetMenuItem")
            .WithTags("Catalog")
            .Produces<MenuItemResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}/items",
                ListMenuItemsAsync)
            .WithName("ListMenuItems")
            .WithTags("Catalog")
            .Produces<IReadOnlyList<MenuItemResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

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

    private static async Task<IResult> GetMenuItemAsync(
        Guid restaurantId,
        Guid categoryId,
        Guid menuItemId,
        IQueryHandler<
            GetMenuItemQuery,
            Result<MenuItemResponse>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.Handle(
            new GetMenuItemQuery(
                restaurantId,
                new(categoryId),
                new(menuItemId)),
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> ListMenuItemsAsync(
        Guid restaurantId,
        Guid categoryId,
        IQueryHandler<
            ListMenuItemsQuery,
            Result<IReadOnlyList<MenuItemResponse>>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.Handle(
            new ListMenuItemsQuery(
                restaurantId,
                new(categoryId)),
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(result.Value);
    }
}

public sealed record CreateMenuItemRequest(
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? Currency,
    int DisplayOrder);

public sealed record CreateMenuItemResponse(Guid Id);
