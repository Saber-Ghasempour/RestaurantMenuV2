using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Application.Restaurants.CreateRestaurant;
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
}

public sealed record CreateRestaurantRequest(string? Name);

public sealed record CreateRestaurantResponse(Guid Id);