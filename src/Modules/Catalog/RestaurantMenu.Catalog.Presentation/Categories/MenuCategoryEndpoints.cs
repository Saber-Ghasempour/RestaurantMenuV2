using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.DeleteMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.ListMenuCategories;
using RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategory;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Presentation.Infrastructure;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Presentation.Categories;

public static class MenuCategoryEndpoints
{
    public static IEndpointRouteBuilder MapMenuCategoryEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost(
                "/api/restaurants/{restaurantId:guid}/categories",
                CreateMenuCategoryAsync)
            .WithName("CreateMenuCategory")
            .WithTags("Catalog")
            .Produces<CreateMenuCategoryResponse>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        endpoints.MapGet(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}",
                GetMenuCategoryAsync)
            .WithName("GetMenuCategory")
            .WithTags("Catalog")
            .Produces<MenuCategoryResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        endpoints.MapPut(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}",
                UpdateMenuCategoryAsync)
            .WithName("UpdateMenuCategory")
            .WithTags("Catalog")
            .Produces<UpdateMenuCategoryResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        endpoints.MapDelete(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}",
                DeleteMenuCategoryAsync)
            .WithName("DeleteMenuCategory")
            .WithTags("Catalog")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        endpoints.MapGet(
                "/api/restaurants/{restaurantId:guid}/categories",
                ListMenuCategoriesAsync)
            .WithName("ListMenuCategories")
            .WithTags("Catalog")
            .Produces<IReadOnlyList<MenuCategoryResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateMenuCategoryAsync(
        Guid restaurantId,
        CreateMenuCategoryRequest request,
        ICommandHandler<
            CreateMenuCategoryCommand,
            Result<MenuCategoryId>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        var command = new CreateMenuCategoryCommand(
            restaurantId,
            request.ParentCategoryId,
            request.Name,
            request.DisplayOrder);
        var result = await handler.Handle(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        var response =
            new CreateMenuCategoryResponse(
                result.Value.Value);

        return Results.Created(
            $"/api/restaurants/{restaurantId}/categories/{response.Id}",
            response);
    }

    private static async Task<IResult> GetMenuCategoryAsync(
        Guid restaurantId,
        Guid categoryId,
        IQueryHandler<
            GetMenuCategoryQuery,
            Result<MenuCategoryResponse>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.Handle(
            new GetMenuCategoryQuery(
                restaurantId,
                new MenuCategoryId(categoryId)),
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> ListMenuCategoriesAsync(
        Guid restaurantId,
        IQueryHandler<
            ListMenuCategoriesQuery,
            Result<IReadOnlyList<MenuCategoryResponse>>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var result = await handler.Handle(
            new ListMenuCategoriesQuery(restaurantId),
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateMenuCategoryAsync(
        Guid restaurantId,
        Guid categoryId,
        UpdateMenuCategoryRequest request,
        ICommandHandler<
            UpdateMenuCategoryCommand,
            Result<long>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        var command = new UpdateMenuCategoryCommand(
            restaurantId,
            new MenuCategoryId(categoryId),
            request.ParentCategoryId,
            request.Name,
            request.DisplayOrder,
            request.ExpectedVersion);
        var result = await handler.Handle(
            command,
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(
                new UpdateMenuCategoryResponse(
                    categoryId,
                    result.Value));
    }

    private static async Task<IResult> DeleteMenuCategoryAsync(
        Guid restaurantId,
        Guid categoryId,
        long expectedVersion,
        ICommandHandler<
            DeleteMenuCategoryCommand,
            Result<MenuCategoryId>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var command = new DeleteMenuCategoryCommand(
            restaurantId,
            new MenuCategoryId(categoryId),
            expectedVersion);
        var result = await handler.Handle(
            command,
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.NoContent();
    }
}

public sealed record CreateMenuCategoryRequest(
    Guid? ParentCategoryId,
    string? Name,
    int DisplayOrder);

public sealed record CreateMenuCategoryResponse(Guid Id);

public sealed record UpdateMenuCategoryRequest(
    Guid? ParentCategoryId,
    string? Name,
    int DisplayOrder,
    long ExpectedVersion);

public sealed record UpdateMenuCategoryResponse(
    Guid Id,
    long Version);
