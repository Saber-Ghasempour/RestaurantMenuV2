using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.PublicMenuCodes;

public sealed class PublicMenuCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MenuOnlyShouldPermitRestaurantOrBranchScopeButNotTableScope()
    {
        Assert.True(Create(PublicMenuCodePurpose.MenuOnly, null, null).IsSuccess);
        Assert.True(Create(PublicMenuCodePurpose.MenuOnly, BranchId.New(), null).IsSuccess);

        var invalid = Create(PublicMenuCodePurpose.MenuOnly, BranchId.New(), DiningTableId.New());
        Assert.True(invalid.IsFailure);
        Assert.Equal(PublicMenuCodeErrors.InvalidPurposeScope, invalid.Error);
    }

    [Fact]
    public void DineInOrderingShouldRequireBranchAndTable()
    {
        var invalid = Create(PublicMenuCodePurpose.DineInOrdering, BranchId.New(), null);
        var valid = Create(PublicMenuCodePurpose.DineInOrdering, BranchId.New(), DiningTableId.New());

        Assert.Equal(PublicMenuCodeErrors.InvalidPurposeScope, invalid.Error);
        Assert.True(valid.IsSuccess);
    }

    [Fact]
    public void RotationAndRevocationShouldBeVersionedAndIdempotent()
    {
        var code = Create(PublicMenuCodePurpose.MenuOnly, null, null).Value;
        code.ClearDomainEvents();

        code.Rotate("new-hash", Now.AddMinutes(1), Now.AddDays(1));
        code.Revoke(Now.AddMinutes(2));
        code.Revoke(Now.AddMinutes(3));

        Assert.Equal("new-hash", code.CodeHash);
        Assert.False(code.IsActive);
        Assert.Equal(3, code.Version);
        Assert.Collection(code.DomainEvents,
            item => Assert.IsType<PublicMenuCodeRotatedDomainEvent>(item),
            item => Assert.IsType<PublicMenuCodeRevokedDomainEvent>(item));
    }

    [Fact]
    public void IsResolvableShouldRequireActiveUnexpiredScope()
    {
        var code = Create(PublicMenuCodePurpose.MenuOnly, null, null, Now.AddMinutes(5)).Value;

        Assert.True(code.IsResolvableAt(Now));
        Assert.False(code.IsResolvableAt(Now.AddMinutes(5)));
    }

    private static RestaurantMenu.SharedKernel.Results.Result<PublicMenuCode> Create(
        PublicMenuCodePurpose purpose, BranchId? branchId, DiningTableId? tableId,
        DateTimeOffset? expiresAtUtc = null) =>
        PublicMenuCode.Create(PublicMenuCodeId.New(), "initial-hash", RestaurantId.New(),
            branchId, tableId, purpose, expiresAtUtc, Now);
}
