using RestaurantMenu.Catalog.Domain.Categories;

namespace RestaurantMenu.Catalog.Domain.UnitTests.Categories;

public sealed class MenuCategoryContentTests
{
    [Fact]
    public void NewCategoryShouldStartAsDraft()
    {
        var category = CreateCategory();

        Assert.Null(category.Description);
        Assert.False(category.IsPublished);
    }

    [Fact]
    public void UpdateContentShouldNormalizeAndValidateDescription()
    {
        var category = CreateCategory();
        category.ClearDomainEvents();

        var result = category.UpdateContent(" Seasonal dishes ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Seasonal dishes", category.Description);
        Assert.Equal(2, category.Version);

        result = category.UpdateContent(new string('a', MenuCategory.MaxDescriptionLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal("Seasonal dishes", category.Description);
        Assert.Equal(2, category.Version);
    }

    [Fact]
    public void ChangePublicationShouldBeIdempotent()
    {
        var category = CreateCategory();
        category.ClearDomainEvents();

        category.ChangePublication(true);
        category.ChangePublication(true);

        Assert.True(category.IsPublished);
        Assert.Equal(2, category.Version);
        Assert.Single(category.DomainEvents);
    }

    private static MenuCategory CreateCategory() =>
        MenuCategory.Create(
            MenuCategoryId.New(), Guid.CreateVersion7(), null, "Food", 1,
            DateTimeOffset.UtcNow).Value;
}
