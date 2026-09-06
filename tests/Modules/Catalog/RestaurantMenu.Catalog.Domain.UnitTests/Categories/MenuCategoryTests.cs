using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Domain.UnitTests.Categories;

public sealed class MenuCategoryTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldBuildCategoryAndRaiseDomainEvent()
    {
        var categoryId = MenuCategoryId.New();
        var restaurantId = Guid.CreateVersion7();
        var parentId = MenuCategoryId.New();

        var result = MenuCategory.Create(
            categoryId,
            restaurantId,
            parentId,
            " Hot Drinks ",
            10,
            CreatedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(categoryId, result.Value.Id);
        Assert.Equal(restaurantId, result.Value.RestaurantId);
        Assert.Equal(parentId, result.Value.ParentId);
        Assert.Equal("Hot Drinks", result.Value.Name);
        Assert.Equal(10, result.Value.DisplayOrder);
        Assert.Equal(CreatedAtUtc, result.Value.CreatedAtUtc);
        Assert.Equal(1, result.Value.Version);

        var domainEvent = Assert.Single(result.Value.DomainEvents);
        var created =
            Assert.IsType<MenuCategoryCreatedDomainEvent>(
                domainEvent);
        Assert.Equal(categoryId, created.MenuCategoryId);
        Assert.Equal(restaurantId, created.RestaurantId);
    }

    [Fact]
    public void CreateShouldRejectEmptyRestaurantId()
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            Guid.Empty,
            null,
            "Drinks",
            0,
            CreatedAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryErrors.RestaurantRequired,
            result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateShouldRejectMissingName(string? name)
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            Guid.CreateVersion7(),
            null,
            name,
            0,
            CreatedAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuCategoryErrors.NameRequired, result.Error);
    }

    [Fact]
    public void CreateShouldRejectNameThatIsTooLong()
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            Guid.CreateVersion7(),
            null,
            new string('a', MenuCategory.MaxNameLength + 1),
            0,
            CreatedAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuCategoryErrors.NameTooLong, result.Error);
    }

    [Fact]
    public void CreateShouldRejectNegativeDisplayOrder()
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            Guid.CreateVersion7(),
            null,
            "Drinks",
            -1,
            CreatedAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuCategoryErrors.InvalidDisplayOrder,
            result.Error);
    }
}
