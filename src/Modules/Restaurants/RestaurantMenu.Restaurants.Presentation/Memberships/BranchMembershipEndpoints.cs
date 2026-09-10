using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Presentation.Abstractions.Authorization;
using RestaurantMenu.Presentation.Abstractions.Results;
using RestaurantMenu.Restaurants.Application.Memberships.AssignBranchMembership;
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
        return endpoints;
    }

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
