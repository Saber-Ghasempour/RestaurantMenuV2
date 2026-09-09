using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Variants;
using RestaurantMenu.Catalog.Application.Variants.ChangeMenuItemVariantAvailability;
using RestaurantMenu.Catalog.Application.Variants.CreateMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.DeleteMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.GetMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.ListMenuItemVariants;
using RestaurantMenu.Catalog.Application.Variants.SetDefaultMenuItemVariant;
using RestaurantMenu.Catalog.Application.Variants.UpdateMenuItemVariant;
using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Presentation.Variants;

public static class MenuItemVariantEndpoints
{
    private const string Route =
        "/api/restaurants/{restaurantId:guid}/categories/{categoryId:guid}/items/{menuItemId:guid}/variants";

    public static IEndpointRouteBuilder MapMenuItemVariantEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost(Route, CreateAsync).WithTags("Catalog")
            .Produces<CreateMenuItemVariantResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem().ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);
        endpoints.MapGet(Route, ListAsync).WithTags("Catalog")
            .Produces<IReadOnlyList<MenuItemVariantResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireRestaurantAccess(Permissions.CatalogRead);
        endpoints.MapGet($"{Route}/{{variantId:guid}}", GetAsync).WithTags("Catalog")
            .Produces<MenuItemVariantResponse>().ProducesProblem(StatusCodes.Status404NotFound)
            .RequireRestaurantAccess(Permissions.CatalogRead);
        endpoints.MapPut($"{Route}/{{variantId:guid}}", UpdateAsync).WithTags("Catalog")
            .Produces<MenuItemVariantMutationResponse>().ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);
        endpoints.MapPatch($"{Route}/{{variantId:guid}}/availability", ChangeAvailabilityAsync)
            .WithTags("Catalog").Produces<MenuItemVariantMutationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);
        endpoints.MapPatch($"{Route}/{{variantId:guid}}/default", SetDefaultAsync)
            .WithTags("Catalog").Produces<MenuItemVariantMutationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);
        endpoints.MapDelete($"{Route}/{{variantId:guid}}", DeleteAsync).WithTags("Catalog")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.CatalogWrite);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        Guid restaurantId, Guid categoryId, Guid menuItemId,
        CreateMenuItemVariantRequest request,
        ICommandHandler<CreateMenuItemVariantCommand, Result<MenuItemVariantId>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateMenuItemVariantCommand(
            restaurantId, new(categoryId), new(menuItemId), request.Name,
            request.Description, request.PriceAmount, request.Currency,
            request.DisplayOrder, request.IsDefault), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Created(
            $"/api/restaurants/{restaurantId}/categories/{categoryId}/items/{menuItemId}/variants/{result.Value.Value}",
            new CreateMenuItemVariantResponse(result.Value.Value));
    }

    private static async Task<IResult> ListAsync(
        Guid restaurantId, Guid categoryId, Guid menuItemId,
        IQueryHandler<ListMenuItemVariantsQuery, Result<IReadOnlyList<MenuItemVariantResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ListMenuItemVariantsQuery(
            restaurantId, new(categoryId), new(menuItemId)), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }

    private static async Task<IResult> GetAsync(
        Guid restaurantId, Guid categoryId, Guid menuItemId, Guid variantId,
        IQueryHandler<GetMenuItemVariantQuery, Result<MenuItemVariantResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetMenuItemVariantQuery(
            restaurantId, new(categoryId), new(menuItemId), new(variantId)),
            cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }

    private static async Task<IResult> UpdateAsync(
        Guid restaurantId, Guid categoryId, Guid menuItemId, Guid variantId,
        UpdateMenuItemVariantRequest request,
        ICommandHandler<UpdateMenuItemVariantCommand, Result<long>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new UpdateMenuItemVariantCommand(
            restaurantId, new(categoryId), new(menuItemId), new(variantId),
            request.Name, request.Description, request.PriceAmount, request.Currency,
            request.DisplayOrder, request.ExpectedVersion), cancellationToken);
        return Mutation(result, variantId);
    }

    private static async Task<IResult> ChangeAvailabilityAsync(
        Guid restaurantId, Guid categoryId, Guid menuItemId, Guid variantId,
        ChangeMenuItemVariantAvailabilityRequest request,
        ICommandHandler<ChangeMenuItemVariantAvailabilityCommand, Result<long>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ChangeMenuItemVariantAvailabilityCommand(
            restaurantId, new(categoryId), new(menuItemId), new(variantId),
            request.IsAvailable, request.ExpectedVersion), cancellationToken);
        return Mutation(result, variantId);
    }

    private static async Task<IResult> SetDefaultAsync(
        Guid restaurantId, Guid categoryId, Guid menuItemId, Guid variantId,
        SetDefaultMenuItemVariantRequest request,
        ICommandHandler<SetDefaultMenuItemVariantCommand, Result<long>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new SetDefaultMenuItemVariantCommand(
            restaurantId, new(categoryId), new(menuItemId), new(variantId),
            request.ExpectedVersion), cancellationToken);
        return Mutation(result, variantId);
    }

    private static async Task<IResult> DeleteAsync(
        Guid restaurantId, Guid categoryId, Guid menuItemId, Guid variantId,
        long expectedVersion,
        ICommandHandler<DeleteMenuItemVariantCommand, Result<MenuItemVariantId>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeleteMenuItemVariantCommand(
            restaurantId, new(categoryId), new(menuItemId), new(variantId),
            expectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.NoContent();
    }

    private static IResult Mutation(Result<long> result, Guid id) =>
        result.IsFailure ? result.Error.ToProblem() :
            Results.Ok(new MenuItemVariantMutationResponse(id, result.Value));
}

public sealed record CreateMenuItemVariantRequest(
    string? Name, string? Description, decimal PriceAmount, string? Currency,
    int DisplayOrder, bool IsDefault);
public sealed record CreateMenuItemVariantResponse(Guid Id);
public sealed record UpdateMenuItemVariantRequest(
    string? Name, string? Description, decimal PriceAmount, string? Currency,
    int DisplayOrder, long ExpectedVersion);
public sealed record ChangeMenuItemVariantAvailabilityRequest(
    bool IsAvailable, long ExpectedVersion);
public sealed record SetDefaultMenuItemVariantRequest(long ExpectedVersion);
public sealed record MenuItemVariantMutationResponse(Guid Id, long Version);
