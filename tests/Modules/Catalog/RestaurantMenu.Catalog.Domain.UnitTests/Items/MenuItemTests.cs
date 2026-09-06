using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Domain.UnitTests.Items;

public sealed class MenuItemTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 7, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldNormalizeValuesAndRaiseDomainEvent()
    {
        var itemId = MenuItemId.New();
        var restaurantId = Guid.CreateVersion7();
        var categoryId = MenuCategoryId.New();

        var result = MenuItem.Create(
            itemId,
            restaurantId,
            categoryId,
            " Carbonara ",
            " Classic pasta ",
            14.50m,
            "eur",
            10,
            CreatedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(itemId, result.Value.Id);
        Assert.Equal(restaurantId, result.Value.RestaurantId);
        Assert.Equal(categoryId, result.Value.CategoryId);
        Assert.Equal("Carbonara", result.Value.Name);
        Assert.Equal("Classic pasta", result.Value.Description);
        Assert.Equal(14.50m, result.Value.Price.Amount);
        Assert.Equal("EUR", result.Value.Price.Currency);
        Assert.Equal(10, result.Value.DisplayOrder);
        Assert.True(result.Value.IsAvailable);
        Assert.Equal(CreatedAtUtc, result.Value.CreatedAtUtc);
        Assert.Equal(1, result.Value.Version);
        var domainEvent = Assert.Single(result.Value.DomainEvents);
        Assert.IsType<MenuItemCreatedDomainEvent>(domainEvent);
    }

    [Fact]
    public void CreateShouldNormalizeBlankDescriptionToNull()
    {
        var result = CreateMenuItem(description: "   ");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Description);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateShouldRejectMissingName(string? name)
    {
        var result = CreateMenuItem(name: name);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuItemErrors.NameRequired, result.Error);
    }

    [Fact]
    public void CreateShouldRejectOverlongDescription()
    {
        var result = CreateMenuItem(
            description: new string(
                'a',
                MenuItem.MaxDescriptionLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(MenuItemErrors.DescriptionTooLong, result.Error);
    }

    [Fact]
    public void CreateShouldRejectNegativeDisplayOrder()
    {
        var result = CreateMenuItem(displayOrder: -1);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuItemErrors.InvalidDisplayOrder, result.Error);
    }

    private static RestaurantMenu.SharedKernel.Results.Result<MenuItem>
        CreateMenuItem(
            string? name = "Item",
            string? description = "Description",
            int displayOrder = 1) =>
        MenuItem.Create(
            MenuItemId.New(),
            Guid.CreateVersion7(),
            MenuCategoryId.New(),
            name,
            description,
            10m,
            "EUR",
            displayOrder,
            CreatedAtUtc);
}
