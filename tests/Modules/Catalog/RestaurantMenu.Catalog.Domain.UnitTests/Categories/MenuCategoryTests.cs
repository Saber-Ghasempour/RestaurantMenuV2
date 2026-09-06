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
        Assert.False(result.Value.IsDeleted);
        Assert.Null(result.Value.DeletedAtUtc);

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

    [Fact]
    public void UpdateShouldChangeFieldsIncrementVersionAndRaiseEvent()
    {
        var category = CreateCategory();
        var parentId = MenuCategoryId.New();
        category.ClearDomainEvents();

        var result = category.Update(
            parentId,
            " Updated Category ",
            20);

        Assert.True(result.IsSuccess);
        Assert.Equal(parentId, category.ParentId);
        Assert.Equal("Updated Category", category.Name);
        Assert.Equal(20, category.DisplayOrder);
        Assert.Equal(2, category.Version);
        var domainEvent = Assert.Single(category.DomainEvents);
        Assert.IsType<MenuCategoryUpdatedDomainEvent>(domainEvent);
    }

    [Fact]
    public void UpdateShouldRejectSelfAsParentWithoutChangingCategory()
    {
        var category = CreateCategory();
        category.ClearDomainEvents();

        var result = category.Update(
            category.Id,
            "Updated Category",
            20);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuCategoryErrors.CannotBeOwnParent, result.Error);
        Assert.Null(category.ParentId);
        Assert.Equal("Original Category", category.Name);
        Assert.Equal(1, category.Version);
        Assert.Empty(category.DomainEvents);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateShouldRejectMissingName(string? name)
    {
        var category = CreateCategory();

        var result = category.Update(null, name, 1);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuCategoryErrors.NameRequired, result.Error);
        Assert.Equal(1, category.Version);
    }

    [Fact]
    public void UpdateShouldBeNoOpWhenValuesAreUnchanged()
    {
        var category = CreateCategory();
        category.ClearDomainEvents();

        var result = category.Update(
            null,
            " Original Category ",
            1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, category.Version);
        Assert.Empty(category.DomainEvents);
    }

    [Fact]
    public void DeleteShouldMarkCategoryIncrementVersionAndRaiseEvent()
    {
        var category = CreateCategory();
        var deletedAtUtc = CreatedAtUtc.AddHours(1);
        category.ClearDomainEvents();

        category.Delete(deletedAtUtc);

        Assert.True(category.IsDeleted);
        Assert.Equal(deletedAtUtc, category.DeletedAtUtc);
        Assert.Equal(2, category.Version);
        var domainEvent = Assert.Single(category.DomainEvents);
        var deleted = Assert.IsType<MenuCategoryDeletedDomainEvent>(
            domainEvent);
        Assert.Equal(deletedAtUtc, deleted.DeletedAtUtc);
    }

    [Fact]
    public void DeleteShouldBeIdempotent()
    {
        var category = CreateCategory();
        category.Delete(CreatedAtUtc.AddHours(1));
        category.ClearDomainEvents();

        category.Delete(CreatedAtUtc.AddHours(2));

        Assert.Equal(CreatedAtUtc.AddHours(1), category.DeletedAtUtc);
        Assert.Equal(2, category.Version);
        Assert.Empty(category.DomainEvents);
    }

    private static MenuCategory CreateCategory()
    {
        var result = MenuCategory.Create(
            MenuCategoryId.New(),
            Guid.CreateVersion7(),
            null,
            "Original Category",
            1,
            CreatedAtUtc);

        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
