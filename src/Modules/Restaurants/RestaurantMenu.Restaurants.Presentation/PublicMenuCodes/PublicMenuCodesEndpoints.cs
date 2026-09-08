using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.CreatePublicMenuCode;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.ListPublicMenuCodes;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.ResolvePublicMenuCode;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.RevokePublicMenuCode;
using RestaurantMenu.Restaurants.Application.PublicMenuCodes.RotatePublicMenuCode;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Presentation.PublicMenuCodes;
public static class PublicMenuCodesEndpoints
{
    public static IEndpointRouteBuilder MapPublicMenuCodesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/restaurants/{restaurantId:guid}/public-menu-codes").WithTags("Public Menu Codes");
        group.MapPost("/", CreateAsync).RequireRestaurantAccess(Permissions.PublicMenuCodesWrite);
        group.MapGet("/", ListAsync).RequireRestaurantAccess(Permissions.PublicMenuCodesRead);
        group.MapPost("/{codeId:guid}/rotate", RotateAsync).RequireRestaurantAccess(Permissions.PublicMenuCodesWrite);
        group.MapPost("/{codeId:guid}/revoke", RevokeAsync).RequireRestaurantAccess(Permissions.PublicMenuCodesWrite);
        endpoints.MapGet("/m/{code}", ResolveAsync).WithName("ResolvePublicMenuCode")
            .AllowAnonymous().RequireRateLimiting("public-menu-code-resolution");
        return endpoints;
    }
    private static async Task<IResult> CreateAsync(Guid restaurantId, CreatePublicMenuCodeRequest request,
        ICommandHandler<CreatePublicMenuCodeCommand, Result<IssuedPublicMenuCode>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(new(restaurantId), request.BranchId is null ? null : new BranchId(request.BranchId.Value),
            request.DiningTableId is null ? null : new DiningTableId(request.DiningTableId.Value), request.Purpose, request.ExpiresAtUtc), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Created($"/api/restaurants/{restaurantId}/public-menu-codes/{result.Value.Id}", result.Value);
    }
    private static async Task<IResult> ListAsync(Guid restaurantId,
        IQueryHandler<ListPublicMenuCodesQuery, Result<IReadOnlyList<PublicMenuCodeResponse>>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(new(restaurantId)), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }
    private static async Task<IResult> RotateAsync(Guid restaurantId, Guid codeId, RotatePublicMenuCodeRequest request,
        ICommandHandler<RotatePublicMenuCodeCommand, Result<IssuedPublicMenuCode>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(new(restaurantId), new(codeId), request.ExpiresAtUtc, request.ExpectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }
    private static async Task<IResult> RevokeAsync(Guid restaurantId, Guid codeId, RevokePublicMenuCodeRequest request,
        ICommandHandler<RevokePublicMenuCodeCommand, Result<long>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(new(restaurantId), new(codeId), request.ExpectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(new PublicMenuCodeMutationResponse(codeId, result.Value));
    }
    private static async Task<IResult> ResolveAsync(string code,
        IQueryHandler<ResolvePublicMenuCodeQuery, Result<ResolvedPublicMenuCode>> handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new(code), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }
}
public sealed record CreatePublicMenuCodeRequest(Guid? BranchId, Guid? DiningTableId,
    PublicMenuCodePurpose Purpose, DateTimeOffset? ExpiresAtUtc);
public sealed record RotatePublicMenuCodeRequest(DateTimeOffset? ExpiresAtUtc, long ExpectedVersion);
public sealed record RevokePublicMenuCodeRequest(long ExpectedVersion);
public sealed record PublicMenuCodeMutationResponse(Guid Id, long Version);
