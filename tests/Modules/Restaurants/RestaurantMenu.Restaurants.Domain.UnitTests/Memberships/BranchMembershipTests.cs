using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.Memberships;

public sealed class BranchMembershipTests
{
    [Fact]
    public void CreateShouldNormalizeSubjectAndOwnExactScope()
    {
        var restaurantId = RestaurantId.New(); var branchId = BranchId.New();
        var result = BranchMembership.Create(restaurantId, branchId, " staff-1 ",
            BranchMembershipRole.Kitchen, DateTimeOffset.UtcNow);
        Assert.True(result.IsSuccess);
        Assert.Equal(restaurantId, result.Value.RestaurantId);
        Assert.Equal(branchId, result.Value.BranchId);
        Assert.Equal("staff-1", result.Value.Subject);
        Assert.Equal(BranchMembershipStatus.Active, result.Value.Status);
    }

    [Fact]
    public void ChangeShouldVersionRoleAndStatusAndTreatNoOpAsIdempotent()
    {
        var membership = BranchMembership.Create(RestaurantId.New(), BranchId.New(), "staff",
            BranchMembershipRole.Waiter, DateTimeOffset.UtcNow).Value;
        Assert.True(membership.Change(BranchMembershipRole.Cashier, BranchMembershipStatus.Suspended).IsSuccess);
        Assert.Equal(2, membership.Version);
        Assert.True(membership.Change(BranchMembershipRole.Cashier, BranchMembershipStatus.Suspended).IsSuccess);
        Assert.Equal(2, membership.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void CreateShouldRejectUnknownRole(int role)
    {
        var result = BranchMembership.Create(RestaurantId.New(), BranchId.New(), "staff",
            (BranchMembershipRole)role, DateTimeOffset.UtcNow);
        Assert.Equal(BranchMembershipErrors.RoleInvalid, result.Error);
    }
}
