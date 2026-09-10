using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IBranchMembershipRepository
{
    void Add(BranchMembership membership);
    Task<BranchMembership?> GetAsync(RestaurantId restaurantId, BranchId branchId,
        string subject, CancellationToken cancellationToken);
}

public interface IBranchMembershipReadService
{
    Task<BranchMembershipAccess?> GetAccessAsync(RestaurantId restaurantId, BranchId branchId,
        string subject, CancellationToken cancellationToken);
}

public sealed record BranchMembershipAccess(BranchMembershipRole Role,
    BranchMembershipStatus Status);
