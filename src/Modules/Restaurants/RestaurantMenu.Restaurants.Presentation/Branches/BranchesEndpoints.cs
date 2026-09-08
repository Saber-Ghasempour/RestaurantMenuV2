using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.Restaurants.Application.Branches.ChangeBranchStatus;
using RestaurantMenu.Restaurants.Application.Branches.CreateBranch;
using RestaurantMenu.Restaurants.Application.Branches.DeleteBranch;
using RestaurantMenu.Restaurants.Application.Branches.GetBranch;
using RestaurantMenu.Restaurants.Application.Branches.ListBranches;
using RestaurantMenu.Restaurants.Application.Branches.UpdateBranch;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Presentation.Branches;

public static class BranchesEndpoints
{
    public static IEndpointRouteBuilder MapBranchesEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        var group = endpoints.MapGroup(
                "/api/restaurants/{restaurantId:guid}/branches")
            .WithTags("Branches");

        group.MapPost("/", CreateAsync)
            .WithName("CreateBranch")
            .Produces<BranchCreatedResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.BranchesWrite);
        group.MapGet("/{branchId:guid}", GetAsync)
            .WithName("GetBranch")
            .Produces<BranchManagementResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .RequireRestaurantAccess(Permissions.BranchesRead);
        group.MapGet("/", ListAsync)
            .WithName("ListBranches")
            .Produces<BranchesListResponse>()
            .ProducesValidationProblem()
            .RequireRestaurantAccess(Permissions.BranchesRead);
        group.MapPut("/{branchId:guid}", UpdateAsync)
            .WithName("UpdateBranch")
            .Produces<BranchMutationResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.BranchesWrite);
        group.MapPatch("/{branchId:guid}/status", ChangeStatusAsync)
            .WithName("ChangeBranchStatus")
            .Produces<BranchMutationResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.BranchesWrite);
        group.MapDelete("/{branchId:guid}", DeleteAsync)
            .WithName("DeleteBranch")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireRestaurantAccess(Permissions.BranchesWrite);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        Guid restaurantId,
        UpsertBranchRequest request,
        ICommandHandler<CreateBranchCommand, Result<BranchId>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new CreateBranchCommand(
            new RestaurantId(restaurantId), request.Name, request.Slug,
            request.Phone, request.AddressLine, request.CityName,
            request.RegionName, request.PostalCode, request.CountryCode,
            request.Latitude, request.Longitude, request.TimeZoneId), cancellationToken);
        if (result.IsFailure) return result.Error.ToProblem();
        var response = new BranchCreatedResponse(result.Value.Value);
        return Results.Created(
            $"/api/restaurants/{restaurantId}/branches/{response.Id}", response);
    }

    private static async Task<IResult> GetAsync(
        Guid restaurantId,
        Guid branchId,
        IQueryHandler<GetBranchQuery, Result<BranchResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new GetBranchQuery(
            new RestaurantId(restaurantId), new BranchId(branchId)), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(Map(result.Value));
    }

    private static async Task<IResult> ListAsync(
        Guid restaurantId,
        IQueryHandler<ListBranchesQuery, Result<BranchesPage>> handler,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        string? search = null,
        bool? isActive = null)
    {
        var result = await handler.Handle(new ListBranchesQuery(
            new RestaurantId(restaurantId), page, pageSize, search, isActive), cancellationToken);
        if (result.IsFailure) return result.Error.ToProblem();
        return Results.Ok(new BranchesListResponse(
            result.Value.Items.Select(Map).ToArray(), result.Value.Page,
            result.Value.PageSize, result.Value.TotalCount, result.Value.TotalPages));
    }

    private static async Task<IResult> UpdateAsync(
        Guid restaurantId,
        Guid branchId,
        UpdateBranchRequest request,
        ICommandHandler<UpdateBranchCommand, Result<long>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new UpdateBranchCommand(
            new RestaurantId(restaurantId), new BranchId(branchId), request.Name,
            request.Slug, request.Phone, request.AddressLine, request.CityName,
            request.RegionName, request.PostalCode, request.CountryCode,
            request.Latitude, request.Longitude, request.TimeZoneId,
            request.ExpectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem()
            : Results.Ok(new BranchMutationResponse(branchId, result.Value));
    }

    private static async Task<IResult> ChangeStatusAsync(
        Guid restaurantId,
        Guid branchId,
        ChangeBranchStatusRequest request,
        ICommandHandler<ChangeBranchStatusCommand, Result<long>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new ChangeBranchStatusCommand(
            new RestaurantId(restaurantId), new BranchId(branchId),
            request.IsActive, request.ExpectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem()
            : Results.Ok(new BranchMutationResponse(branchId, result.Value));
    }

    private static async Task<IResult> DeleteAsync(
        Guid restaurantId,
        Guid branchId,
        long expectedVersion,
        ICommandHandler<DeleteBranchCommand, Result<BranchId>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new DeleteBranchCommand(
            new RestaurantId(restaurantId), new BranchId(branchId), expectedVersion),
            cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.NoContent();
    }

    private static BranchManagementResponse Map(BranchResponse branch) => new(
        branch.Id, branch.RestaurantId, branch.Name, branch.Slug, branch.Phone,
        branch.AddressLine, branch.CityName, branch.RegionName, branch.PostalCode,
        branch.CountryCode, branch.Latitude, branch.Longitude, branch.TimeZoneId,
        branch.IsActive, branch.CreatedAtUtc, branch.Version);
}

public record UpsertBranchRequest(
    string? Name, string? Slug, string? Phone, string? AddressLine,
    string? CityName, string? RegionName, string? PostalCode,
    string? CountryCode, decimal? Latitude, decimal? Longitude,
    string? TimeZoneId);

public sealed record UpdateBranchRequest(
    string? Name, string? Slug, string? Phone, string? AddressLine,
    string? CityName, string? RegionName, string? PostalCode,
    string? CountryCode, decimal? Latitude, decimal? Longitude,
    string? TimeZoneId, long ExpectedVersion)
    : UpsertBranchRequest(Name, Slug, Phone, AddressLine, CityName, RegionName,
        PostalCode, CountryCode, Latitude, Longitude, TimeZoneId);

public sealed record ChangeBranchStatusRequest(bool IsActive, long ExpectedVersion);
public sealed record BranchCreatedResponse(Guid Id);
public sealed record BranchMutationResponse(Guid Id, long Version);
public sealed record BranchManagementResponse(
    Guid Id, Guid RestaurantId, string Name, string? Slug, string? Phone,
    string? AddressLine, string? CityName, string? RegionName, string? PostalCode,
    string? CountryCode, decimal? Latitude, decimal? Longitude, string? TimeZoneId,
    bool IsActive, DateTimeOffset CreatedAtUtc, long Version);
public sealed record BranchesListResponse(
    IReadOnlyList<BranchManagementResponse> Items, int Page, int PageSize,
    int TotalCount, int TotalPages);
