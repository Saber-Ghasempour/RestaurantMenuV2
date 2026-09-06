using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Catalog.Application.Categories.CreateMenuCategory;
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
}

public sealed record CreateMenuCategoryRequest(
    Guid? ParentCategoryId,
    string? Name,
    int DisplayOrder);

public sealed record CreateMenuCategoryResponse(Guid Id);
