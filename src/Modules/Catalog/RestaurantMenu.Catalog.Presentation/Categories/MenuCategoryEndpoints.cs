using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.ListMenuCategories;
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
}

public sealed record CreateMenuCategoryRequest(
    Guid? ParentCategoryId,
    string? Name,
    int DisplayOrder);

public sealed record CreateMenuCategoryResponse(Guid Id);
