using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Memberships.AssignBranchMembership;

public sealed record AssignBranchMembershipCommand(RestaurantId RestaurantId, BranchId BranchId,
    string Subject, BranchMembershipRole Role, BranchMembershipStatus Status, long? ExpectedVersion)
    : ICommand<Result<BranchMembershipResponse>>;

public sealed record BranchMembershipResponse(Guid RestaurantId, Guid BranchId, string Subject,
    string Role, string Status, long Version);
