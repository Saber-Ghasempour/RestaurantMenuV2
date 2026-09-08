using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Publications;

namespace RestaurantMenu.Catalog.Domain.UnitTests.Publications;

public sealed class BranchCategoryPublicationTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 8, 19, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldCaptureTenantScopeAndPublicationState()
    {
        var restaurantId = Guid.CreateVersion7();
        var branchId = Guid.CreateVersion7();
        var categoryId = MenuCategoryId.New();

        var result = BranchCategoryPublication.Create(
            restaurantId,
            branchId,
            categoryId,
            isPublished: true,
            displayOrderOverride: 3,
            CreatedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(restaurantId, result.Value.RestaurantId);
        Assert.Equal(branchId, result.Value.BranchId);
        Assert.Equal(categoryId, result.Value.CategoryId);
        Assert.True(result.Value.IsPublished);
        Assert.Equal(3, result.Value.DisplayOrderOverride);
        Assert.Equal(1, result.Value.Version);
    }

    [Fact]
    public void SetShouldBeIdempotentAndRejectNegativeOrder()
    {
        var result = BranchCategoryPublication.Create(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            MenuCategoryId.New(),
            isPublished: true,
            displayOrderOverride: null,
            CreatedAtUtc);
        Assert.True(result.IsSuccess);

        var noOp = result.Value.Set(true, null);
        var invalid = result.Value.Set(true, -1);

        Assert.True(noOp.IsSuccess);
        Assert.True(invalid.IsFailure);
        Assert.Equal(1, result.Value.Version);
        Assert.Null(result.Value.DisplayOrderOverride);
        Assert.Empty(result.Value.DomainEvents);
    }
}
