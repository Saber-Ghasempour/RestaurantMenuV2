using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Items.ChangeMenuItemAvailability;
using RestaurantMenu.Catalog.Application.Items.CreateMenuItem;
using RestaurantMenu.Catalog.Application.Items.GetMenuItem;
using RestaurantMenu.Catalog.Application.Items.ListMenuItems;
using RestaurantMenu.Catalog.Application.Items.UpdateMenuItem;
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

        endpoints.MapPut(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}/items/{menuItemId:guid}",
                UpdateMenuItemAsync)
            .WithName("UpdateMenuItem")
            .WithTags("Catalog")
            .Produces<UpdateMenuItemResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapPatch(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}/items/{menuItemId:guid}/availability",
                ChangeMenuItemAvailabilityAsync)
            .WithName("ChangeMenuItemAvailability")
            .WithTags("Catalog")
            .Produces<ChangeMenuItemAvailabilityResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

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

    private static async Task<IResult> UpdateMenuItemAsync(
        Guid restaurantId,
        Guid categoryId,
        Guid menuItemId,
        UpdateMenuItemRequest request,
        ICommandHandler<
            UpdateMenuItemCommand,
            Result<long>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.Handle(
            new UpdateMenuItemCommand(
                restaurantId,
                new(categoryId),
                new(menuItemId),
                request.Name,
                request.Description,
                request.PriceAmount,
                request.Currency,
                request.DisplayOrder,
                request.ExpectedVersion),
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(
                new UpdateMenuItemResponse(
                    menuItemId,
                    result.Value));
    }

    private static async Task<IResult> ChangeMenuItemAvailabilityAsync(
        Guid restaurantId,
        Guid categoryId,
        Guid menuItemId,
        ChangeMenuItemAvailabilityRequest request,
        ICommandHandler<
            ChangeMenuItemAvailabilityCommand,
            Result<long>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.Handle(
            new ChangeMenuItemAvailabilityCommand(
                restaurantId,
                new(categoryId),
                new(menuItemId),
                request.IsAvailable,
                request.ExpectedVersion),
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(
                new ChangeMenuItemAvailabilityResponse(
                    menuItemId,
                    request.IsAvailable,
                    result.Value));
    }
}

public sealed record CreateMenuItemRequest(
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? Currency,
    int DisplayOrder);

public sealed record CreateMenuItemResponse(Guid Id);

public sealed record UpdateMenuItemRequest(
    string? Name,
    string? Description,
    decimal PriceAmount,
    string? Currency,
    int DisplayOrder,
    long ExpectedVersion);

public sealed record UpdateMenuItemResponse(
    Guid Id,
    long Version);

public sealed record ChangeMenuItemAvailabilityRequest(
    bool IsAvailable,
    long ExpectedVersion);

public sealed record ChangeMenuItemAvailabilityResponse(
    Guid Id,
    bool IsAvailable,
    long Version);
