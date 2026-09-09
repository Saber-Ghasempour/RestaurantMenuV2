using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.DeleteMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.GetMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.ListMenuCategories;
using RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategory;
using RestaurantMenu.Catalog.Application.Categories.UpdateMenuCategoryContent;
using RestaurantMenu.Catalog.Application.Categories.ChangeMenuCategoryPublication;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
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
                StatusCodes.Status404NotFound)
            .RequireRestaurantAccess(Permissions.CatalogWrite);

        endpoints.MapGet(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}",
                GetMenuCategoryAsync)
            .WithName("GetMenuCategory")
            .WithTags("Catalog")
            .Produces<MenuCategoryResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireRestaurantAccess(Permissions.CatalogRead);

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
                StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);

        endpoints.MapPut(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}/content",
                UpdateMenuCategoryContentAsync)
            .WithName("UpdateMenuCategoryContent")
            .WithTags("Catalog")
            .Produces<UpdateMenuCategoryResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);

        endpoints.MapPatch(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}/publication",
                ChangeMenuCategoryPublicationAsync)
            .WithName("ChangeMenuCategoryPublication")
            .WithTags("Catalog")
            .Produces<ChangeMenuCategoryPublicationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);

        endpoints.MapDelete(
                "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}",
                DeleteMenuCategoryAsync)
            .WithName("DeleteMenuCategory")
            .WithTags("Catalog")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);

        endpoints.MapGet(
                "/api/restaurants/{restaurantId:guid}/categories",
                ListMenuCategoriesAsync)
            .WithName("ListMenuCategories")
            .WithTags("Catalog")
            .Produces<IReadOnlyList<MenuCategoryResponse>>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireRestaurantAccess(Permissions.CatalogRead);

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

    private static async Task<IResult> UpdateMenuCategoryContentAsync(
        Guid restaurantId,
        Guid categoryId,
        UpdateMenuCategoryContentRequest request,
        ICommandHandler<UpdateMenuCategoryContentCommand, Result<long>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new UpdateMenuCategoryContentCommand(
            restaurantId, new MenuCategoryId(categoryId), request.Description,
            request.ExpectedVersion), cancellationToken);
        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(new UpdateMenuCategoryResponse(categoryId, result.Value));
    }

    private static async Task<IResult> ChangeMenuCategoryPublicationAsync(
        Guid restaurantId,
        Guid categoryId,
        ChangeMenuCategoryPublicationRequest request,
        ICommandHandler<ChangeMenuCategoryPublicationCommand, Result<long>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ChangeMenuCategoryPublicationCommand(
            restaurantId, new MenuCategoryId(categoryId), request.IsPublished,
            request.ExpectedVersion), cancellationToken);
        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(new ChangeMenuCategoryPublicationResponse(
                categoryId, request.IsPublished, result.Value));
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

public sealed record UpdateMenuCategoryContentRequest(
    string? Description,
    long ExpectedVersion);

public sealed record ChangeMenuCategoryPublicationRequest(
    bool IsPublished,
    long ExpectedVersion);

public sealed record ChangeMenuCategoryPublicationResponse(
    Guid Id,
    bool IsPublished,
    long Version);

public sealed record UpdateMenuCategoryResponse(
    Guid Id,
    long Version);
