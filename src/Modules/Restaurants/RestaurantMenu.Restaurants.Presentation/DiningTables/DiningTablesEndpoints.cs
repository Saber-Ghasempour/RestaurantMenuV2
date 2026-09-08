using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.Restaurants.Application.DiningTables;
using RestaurantMenu.Restaurants.Application.DiningTables.ChangeDiningTableStatus;
using RestaurantMenu.Restaurants.Application.DiningTables.CreateDiningTable;
using RestaurantMenu.Restaurants.Application.DiningTables.ListDiningTables;
using RestaurantMenu.Restaurants.Application.DiningTables.UpdateDiningTable;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Presentation.DiningTables;
public static class DiningTablesEndpoints
{
    public static IEndpointRouteBuilder MapDiningTablesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/restaurants/{restaurantId:guid}/branches/{branchId:guid}/tables").WithTags("Dining Tables");
        group.MapPost("/", CreateAsync).RequireRestaurantAccess(Permissions.DiningTablesWrite);
        group.MapGet("/", ListAsync).RequireRestaurantAccess(Permissions.DiningTablesRead);
        group.MapPut("/{tableId:guid}", UpdateAsync).RequireRestaurantAccess(Permissions.DiningTablesWrite);
        group.MapPatch("/{tableId:guid}/status", StatusAsync).RequireRestaurantAccess(Permissions.DiningTablesWrite);
        return endpoints;
    }
    private static async Task<IResult> CreateAsync(Guid restaurantId, Guid branchId, UpsertDiningTableRequest request,
        ICommandHandler<CreateDiningTableCommand, Result<DiningTableId>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(new(restaurantId), new(branchId), request.Number, request.DisplayName, request.Capacity), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Created($"/api/restaurants/{restaurantId}/branches/{branchId}/tables/{result.Value.Value}", new DiningTableCreatedResponse(result.Value.Value));
    }
    private static async Task<IResult> ListAsync(Guid restaurantId, Guid branchId,
        IQueryHandler<ListDiningTablesQuery, Result<IReadOnlyList<DiningTableResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(new(restaurantId), new(branchId)), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }
    private static async Task<IResult> UpdateAsync(Guid restaurantId, Guid branchId, Guid tableId, UpdateDiningTableRequest request,
        ICommandHandler<UpdateDiningTableCommand, Result<long>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(new(restaurantId), new(branchId), new(tableId), request.Number,
            request.DisplayName, request.Capacity, request.ExpectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(new DiningTableMutationResponse(tableId, result.Value));
    }
    private static async Task<IResult> StatusAsync(Guid restaurantId, Guid branchId, Guid tableId, ChangeDiningTableStatusRequest request,
        ICommandHandler<ChangeDiningTableStatusCommand, Result<long>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(new(restaurantId), new(branchId), new(tableId), request.IsActive,
            request.ExpectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(new DiningTableMutationResponse(tableId, result.Value));
    }
}
public record UpsertDiningTableRequest(int Number, string? DisplayName, short? Capacity);
public sealed record UpdateDiningTableRequest(int Number, string? DisplayName, short? Capacity, long ExpectedVersion)
    : UpsertDiningTableRequest(Number, DisplayName, Capacity);
public sealed record ChangeDiningTableStatusRequest(bool IsActive, long ExpectedVersion);
public sealed record DiningTableCreatedResponse(Guid Id);
public sealed record DiningTableMutationResponse(Guid Id, long Version);
