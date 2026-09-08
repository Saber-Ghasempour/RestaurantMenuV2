using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantLinks;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurantProfile;
using RestaurantMenu.Application.Abstractions.Security;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.Restaurants.Application.Restaurants.CreateRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.DeleteRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.GetRestaurant;
using RestaurantMenu.Restaurants.Application.Restaurants.ListRestaurants;
using RestaurantMenu.Restaurants.Application.Restaurants.UpdateRestaurant;
using RestaurantMenu.Restaurants.Domain.Restaurants;
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
                StatusCodes.Status400BadRequest)
            .RequirePermission(Permissions.RestaurantsWrite);

        group.MapGet(
                "/{restaurantId:guid}",
                GetRestaurantAsync)
            .WithName("GetRestaurant")
            .Produces<GetRestaurantResponse>(
                StatusCodes.Status200OK)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .RequireRestaurantAccess(Permissions.RestaurantsRead);

        group.MapGet(
                "/",
                ListRestaurantsAsync)
            .WithName("ListRestaurants")
            .Produces<ListRestaurantsResponse>(
                StatusCodes.Status200OK)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .RequirePermission(Permissions.RestaurantsRead);

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
                StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.RestaurantsWrite);

        group.MapDelete(
                "/{restaurantId:guid}",
                DeleteRestaurantAsync)
            .WithName("DeleteRestaurant")
            .Produces(
                StatusCodes.Status204NoContent)
            .ProducesValidationProblem(
                StatusCodes.Status400BadRequest)
            .ProducesProblem(
                StatusCodes.Status404NotFound)
            .ProducesProblem(
                StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.RestaurantsWrite);

        group.MapPut("/{restaurantId:guid}/profile", UpdateProfileAsync)
            .WithName("UpdateRestaurantProfile")
            .Produces<UpdateRestaurantResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.RestaurantsWrite);

        group.MapPut("/{restaurantId:guid}/links", UpdateLinksAsync)
            .WithName("UpdateRestaurantLinks")
            .Produces<UpdateRestaurantResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.RestaurantsWrite);

        return endpoints;
    }

    private static async Task<IResult> UpdateProfileAsync(
        Guid restaurantId,
        UpdateRestaurantProfileRequest request,
        ICommandHandler<UpdateRestaurantProfileCommand, Result<long>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new UpdateRestaurantProfileCommand(
                new RestaurantId(restaurantId), request.Description,
                request.About, request.Address, request.ExpectedVersion),
            cancellationToken);
        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(new UpdateRestaurantResponse(restaurantId, result.Value));
    }

    private static async Task<IResult> UpdateLinksAsync(
        Guid restaurantId,
        UpdateRestaurantLinksRequest request,
        ICommandHandler<UpdateRestaurantLinksCommand, Result<long>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);
        var result = await handler.Handle(
            new UpdateRestaurantLinksCommand(
                new RestaurantId(restaurantId),
                request.WebsiteUrl,
                request.InstagramUrl,
                request.FacebookUrl,
                request.WhatsAppUrl,
                request.TelegramUrl,
                request.TwitterUrl,
                request.ExpectedVersion),
            cancellationToken);
        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.Ok(new UpdateRestaurantResponse(restaurantId, result.Value));
    }

    private static async Task<IResult> CreateRestaurantAsync(
        CreateRestaurantRequest request,
        ICurrentUser currentUser,
        ICommandHandler<
            CreateRestaurantCommand,
            Result<RestaurantId>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handler);

        var command =
            new CreateRestaurantCommand(
                request.Name,
                currentUser.Subject);

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
                result.Value.Version,
                result.Value.Description,
                result.Value.About,
                result.Value.Address,
                result.Value.WebsiteUrl,
                result.Value.InstagramUrl,
                result.Value.FacebookUrl,
                result.Value.WhatsAppUrl,
                result.Value.TelegramUrl,
                result.Value.TwitterUrl);

        return Results.Ok(response);
    }

    private static async Task<IResult> ListRestaurantsAsync(
        ICurrentUser currentUser,
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
                currentUser.Subject,
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

    private static async Task<IResult> DeleteRestaurantAsync(
        Guid restaurantId,
        long expectedVersion,
        ICommandHandler<
            DeleteRestaurantCommand,
            Result<RestaurantId>> handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var command = new DeleteRestaurantCommand(
            new RestaurantId(restaurantId),
            expectedVersion);

        var result = await handler.Handle(
            command,
            cancellationToken);

        return result.IsFailure
            ? result.Error.ToProblem()
            : Results.NoContent();
    }
}

public sealed record UpdateRestaurantLinksRequest(
    string? WebsiteUrl,
    string? InstagramUrl,
    string? FacebookUrl,
    string? WhatsAppUrl,
    string? TelegramUrl,
    string? TwitterUrl,
    long ExpectedVersion);

public sealed record CreateRestaurantRequest(string? Name);

public sealed record CreateRestaurantResponse(Guid Id);

public sealed record GetRestaurantResponse(
    Guid Id,
    string Name,
    DateTimeOffset CreatedAtUtc,
    long Version,
    string? Description,
    string? About,
    string? Address,
    string? WebsiteUrl,
    string? InstagramUrl,
    string? FacebookUrl,
    string? WhatsAppUrl,
    string? TelegramUrl,
    string? TwitterUrl);

public sealed record UpdateRestaurantProfileRequest(
    string? Description,
    string? About,
    string? Address,
    long ExpectedVersion);

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
