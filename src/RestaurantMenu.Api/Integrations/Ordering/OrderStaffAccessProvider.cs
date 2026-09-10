using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Api.Integrations.Ordering;

public sealed class OrderStaffAccessProvider(IBranchMembershipReadService memberships)
    : IOrderStaffAccessProvider
{
    public async Task<OrderStaffRole?> GetActiveRoleAsync(Guid restaurantId, Guid branchId,
        string subject, CancellationToken cancellationToken)
    {
        var access = await memberships.GetAccessAsync(new RestaurantId(restaurantId),
            new BranchId(branchId), subject, cancellationToken);
        if (access?.Status != BranchMembershipStatus.Active) return null;
        return access.Role switch
        {
            BranchMembershipRole.Manager => OrderStaffRole.Manager,
            BranchMembershipRole.Cashier => OrderStaffRole.Cashier,
            BranchMembershipRole.Kitchen => OrderStaffRole.Kitchen,
            BranchMembershipRole.Waiter => OrderStaffRole.Waiter,
            _ => null
        };
    }
}
