using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Domain.UnitTests.Items;

public sealed class MenuItemMetadataTests
{
    [Fact]
    public void NewItemShouldStartWithEmptyMetadataAndAsDraft()
    {
        var item = CreateItem();

        Assert.Null(item.Recipe);
        Assert.Null(item.Calories);
        Assert.Empty(item.Tags);
        Assert.Null(item.AllergenNotes);
        Assert.Null(item.PreparationTimeMinutes);
        Assert.False(item.IsFeatured);
        Assert.False(item.IsPublished);
    }

    [Fact]
    public void UpdateMetadataShouldNormalizeValuesAndTags()
    {
        var item = CreateItem();
        item.ClearDomainEvents();

        var result = item.UpdateMetadata(
            " Tomatoes and basil ", 0,
            [" Vegan ", "quick", "VEGAN"], " Contains nuts ", 15, true);

        Assert.True(result.IsSuccess);
        Assert.Equal("Tomatoes and basil", item.Recipe);
        Assert.Equal(0, item.Calories);
        Assert.Equal(["quick", "vegan"], item.Tags);
        Assert.Equal("Contains nuts", item.AllergenNotes);
        Assert.Equal((short)15, item.PreparationTimeMinutes);
        Assert.True(item.IsFeatured);
        Assert.Equal(2, item.Version);
        Assert.IsType<MenuItemMetadataUpdatedDomainEvent>(Assert.Single(item.DomainEvents));
    }

    [Theory]
    [InlineData(2001, 10, 10, "Catalog.MenuItemRecipeTooLong")]
    [InlineData(10, -1, 10, "Catalog.MenuItemInvalidCalories")]
    [InlineData(10, 10, 0, "Catalog.MenuItemInvalidPreparationTime")]
    public void UpdateMetadataShouldRejectInvalidValuesAtomically(
        int recipeLength,
        int calories,
        int preparationMinutes,
        string expectedCode)
    {
        var item = CreateItem();

        var result = item.UpdateMetadata(
            new string('a', recipeLength), calories, [], null,
            preparationMinutes, false);

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
        Assert.Null(item.Recipe);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void UpdateMetadataShouldRejectInvalidTags()
    {
        var item = CreateItem();

        var tooMany = item.UpdateMetadata(null, null,
            Enumerable.Range(0, MenuItem.MaxTagCount + 1).Select(index => $"tag-{index}").ToArray(),
            null, null, false);
        var tooLong = item.UpdateMetadata(null, null,
            [new string('a', MenuItem.MaxTagLength + 1)], null, null, false);

        Assert.Equal("Catalog.MenuItemTooManyTags", tooMany.Error.Code);
        Assert.Equal("Catalog.MenuItemTagTooLong", tooLong.Error.Code);
        Assert.Empty(item.Tags);
        Assert.Equal(1, item.Version);
    }

    [Fact]
    public void ChangePublicationShouldBeIdempotent()
    {
        var item = CreateItem();
        item.ClearDomainEvents();

        item.ChangePublication(true);
        item.ChangePublication(true);

        Assert.True(item.IsPublished);
        Assert.Equal(2, item.Version);
        Assert.Single(item.DomainEvents);
    }

    private static MenuItem CreateItem() =>
        MenuItem.Create(
            MenuItemId.New(), Guid.CreateVersion7(), MenuCategoryId.New(),
            "Soup", null, 1, DateTimeOffset.UtcNow).Value;
}
