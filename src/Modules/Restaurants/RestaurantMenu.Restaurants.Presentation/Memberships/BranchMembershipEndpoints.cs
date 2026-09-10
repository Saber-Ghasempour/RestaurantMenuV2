using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.Restaurants.Application.Memberships.AssignBranchMembership;
using RestaurantMenu.Restaurants.Application.Memberships;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Presentation.Memberships;

public static class BranchMembershipEndpoints
{
    public static IEndpointRouteBuilder MapBranchMembershipEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/restaurants/{restaurantId:guid}/branches/{branchId:guid}/members/{subject}", AssignAsync)
            .WithTags("Branch Memberships").Produces<BranchMembershipResponse>()
            .ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409)
            .RequireRestaurantAccess(Permissions.BranchesWrite);
        endpoints.MapPost("/api/restaurants/{restaurantId:guid}/member-invitations", InviteAsync).WithTags("Memberships").RequireRestaurantAccess(Permissions.MembersWrite);
        endpoints.MapPost("/api/public/member-invitations/accept", AcceptAsync).WithTags("Memberships").AllowAnonymous().RequireRateLimiting("dining-session-start");
        endpoints.MapPut("/api/restaurants/{restaurantId:guid}/members/{subject}", ChangeAsync).WithTags("Memberships").RequireRestaurantAccess(Permissions.MembersWrite);
        endpoints.MapGet("/api/restaurants/{restaurantId:guid}/members", ListMembersAsync).WithTags("Memberships").RequireRestaurantAccess(Permissions.MembersRead);
        endpoints.MapGet("/api/restaurants/{restaurantId:guid}/member-invitations", ListInvitationsAsync).WithTags("Memberships").RequireRestaurantAccess(Permissions.MembersRead);
        endpoints.MapPost("/api/restaurants/{restaurantId:guid}/member-invitations/{invitationId:guid}/revoke", RevokeAsync).WithTags("Memberships").RequireRestaurantAccess(Permissions.MembersWrite);
        endpoints.MapGet("/api/restaurants/{restaurantId:guid}/membership-audit", ListAuditAsync).WithTags("Memberships").RequireRestaurantAccess(Permissions.MembersRead);
        return endpoints;
    }
    private static async Task<IResult> InviteAsync(Guid restaurantId, InviteRequest request, ICommandHandler<InviteMemberCommand, Result<InvitationResponse>> handler, CancellationToken cancellationToken) { var r = await handler.Handle(new(new(restaurantId), request.Email, request.Role), cancellationToken); return r.IsFailure ? r.Error.ToProblem() : Results.Created($"/api/restaurants/{restaurantId}/member-invitations/{r.Value.Id}", r.Value); }
    private static async Task<IResult> AcceptAsync(AcceptRequest request, ICommandHandler<AcceptInvitationCommand, Result<MemberResponse>> handler, CancellationToken cancellationToken) { var r = await handler.Handle(new(request.Token), cancellationToken); return r.IsFailure ? r.Error.ToProblem() : Results.Ok(r.Value); }
    private static async Task<IResult> ChangeAsync(Guid restaurantId, string subject, ChangeMemberRequest request, ICommandHandler<ChangeMemberCommand, Result<MemberResponse>> handler, CancellationToken cancellationToken) { var r = await handler.Handle(new(new(restaurantId), subject, request.Role, request.Status, request.ExpectedVersion), cancellationToken); return r.IsFailure ? r.Error.ToProblem() : Results.Ok(r.Value); }
    private static async Task<IResult> ListMembersAsync(Guid restaurantId, IQueryHandler<ListMembersQuery, Result<IReadOnlyList<MemberResponse>>> h, CancellationToken ct) => ToResult(await h.Handle(new(new(restaurantId)), ct));
    private static async Task<IResult> ListInvitationsAsync(Guid restaurantId, IQueryHandler<ListInvitationsQuery, Result<IReadOnlyList<InvitationResponse>>> h, CancellationToken ct) => ToResult(await h.Handle(new(new(restaurantId)), ct));
    private static async Task<IResult> ListAuditAsync(Guid restaurantId, IQueryHandler<ListMembershipAuditQuery, Result<IReadOnlyList<AuditResponse>>> h, CancellationToken ct) => ToResult(await h.Handle(new(new(restaurantId)), ct));
    private static async Task<IResult> RevokeAsync(Guid restaurantId, Guid invitationId, VersionOnlyRequest request, ICommandHandler<RevokeInvitationCommand, Result<InvitationResponse>> h, CancellationToken ct) => ToResult(await h.Handle(new(new(restaurantId), new(invitationId), request.ExpectedVersion), ct));
    private static IResult ToResult<T>(Result<T> r) where T : notnull => r.IsFailure ? r.Error.ToProblem() : Results.Ok(r.Value);

    private static async Task<IResult> AssignAsync(Guid restaurantId, Guid branchId, string subject,
        AssignBranchMembershipRequest request,
        ICommandHandler<AssignBranchMembershipCommand, Result<BranchMembershipResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new AssignBranchMembershipCommand(new RestaurantId(restaurantId),
            new BranchId(branchId), subject, request.Role, request.Status, request.ExpectedVersion), cancellationToken);
        return result.IsFailure ? result.Error.ToProblem() : Results.Ok(result.Value);
    }
}

public sealed record AssignBranchMembershipRequest(BranchMembershipRole Role,
    BranchMembershipStatus Status = BranchMembershipStatus.Active, long? ExpectedVersion = null);
public sealed record InviteRequest(string Email, RestaurantMembershipRole Role);
public sealed record AcceptRequest(string Token);
public sealed record ChangeMemberRequest(RestaurantMembershipRole Role, RestaurantMembershipStatus Status, long ExpectedVersion);
public sealed record VersionOnlyRequest(long ExpectedVersion);