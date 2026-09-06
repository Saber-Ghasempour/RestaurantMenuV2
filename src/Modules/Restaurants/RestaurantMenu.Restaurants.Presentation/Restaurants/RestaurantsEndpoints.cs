using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Restaurants.CreateRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Presentation.Infrastructure;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Presentation.Restaurants;

public static class RestaurantsEndpoints
{
    public static IEndpointRouteBuilder MapRestaurantsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints
            .MapGroup("/api/restaurants")
            .WithTags("Restaurants");

        group.MapPost(
                "/",
                CreateRestaurantAsync)
            .WithName("CreateRestaurant")
            .Produces<CreateRestaurantResponse>(
                StatusCodes.Status201Created)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest);

        group.MapGet(
                "/{restaurantId:guid}",
                GetRestaurantAsync)
            .WithName("GetRestaurant")
            .Produces<GetRestaurantResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound);

        group.MapGet(
                "/",
                ListRestaurantsAsync)
            .WithName("ListRestaurants")
            .Produces<ListRestaurantsResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest);

        group.MapPut(
                "/{restaurantId:guid}",
                UpdateRestaurantAsync)
            .WithName("UpdateRestaurant")
            .Produces<UpdateRestaurantResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict);

        return endpoints;
    }

    private static async Task<IResult> CreateRestaurantAsync(
        CreateRestaurantRequest request,
        ICommandHandler<
            CreateRestaurantCommand,
            Result<RestaurantId>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        var command =
            new CreateRestaurantCommand(request.Name);

        var result =
            await handler.Handle(
                command,
                cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        var response =
            new CreateRestaurantResponse(
                result.Value.Value);

        return Results.Created(
            $"/api/restaurants/{response.Id}",
            response);
    }

    private static async Task<IResult> GetRestaurantAsync(
        Guid restaurantId,
        IQueryHandler<
            GetRestaurantQuery,
            Result<RestaurantResponse>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var query =
            new GetRestaurantQuery(
                new RestaurantId(restaurantId));

        var result =
            await handler.Handle(
                query,
                cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        var response =
            new GetRestaurantResponse(
                result.Value.Id,
                result.Value.Name,
                result.Value.CreatedAtUtc,
                result.Value.Version);

        return Results.Ok(response);
    }

    private static async Task<IResult> ListRestaurantsAsync(
        IQueryHandler<
            ListRestaurantsQuery,
            Result<RestaurantsPage>> handler,
        CancellationToken cancellationToken,
        int page = ListRestaurantsQuery.DefaultPage,
        int pageSize = ListRestaurantsQuery.DefaultPageSize)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var query =
            new ListRestaurantsQuery(
                page,
                pageSize);

        var result =
            await handler.Handle(
                query,
                cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        var items =
            result.Value.Items
                .Select(
                    restaurant =>
                        new RestaurantListItemResponse(
                            restaurant.Id,
                            restaurant.Name,
                            restaurant.CreatedAtUtc,
                            restaurant.Version))
                .ToArray();

        var response =
            new ListRestaurantsResponse(
                items,
                result.Value.Page,
                result.Value.PageSize,
                result.Value.TotalCount,
                result.Value.TotalPages);

        return Results.Ok(response);
    }

    private static async Task<IResult> UpdateRestaurantAsync(
        Guid restaurantId,
        UpdateRestaurantRequest request,
        ICommandHandler<
            UpdateRestaurantCommand,
            Result<long>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        var command = new UpdateRestaurantCommand(
            new RestaurantId(restaurantId),
            request.Name,
            request.ExpectedVersion);

        var result = await handler.Handle(
            command,
            cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToProblem();
        }

        return Results.Ok(
            new UpdateRestaurantResponse(
                restaurantId,
                result.Value));
    }
}

public sealed record CreateRestaurantRequest(string? Name);

public sealed record CreateRestaurantResponse(Guid Id);

public sealed record GetRestaurantResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    long Version);

public sealed record ListRestaurantsResponse(
    IReadOnlyList<RestaurantListItemResponse> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record RestaurantListItemResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    long Version);

public sealed record UpdateRestaurantRequest(
    string? Name,
    long ExpectedVersion);

public sealed record UpdateRestaurantResponse(
    Guid Id,
    long Version);
