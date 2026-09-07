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

    [Fact]
    public void UpdateShouldNormalizeValuesIncrementVersionAndRaiseEvent()
    {
        var menuItem = CreateMenuItem().Value;
        menuItem.ClearDomainEvents();

        var result = menuItem.Update(
            " Updated Item ",
            " Updated description ",
            12.75m,
            "usd",
            20);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Item", menuItem.Name);
        Assert.Equal("Updated description", menuItem.Description);
        Assert.Equal(12.75m, menuItem.Price.Amount);
        Assert.Equal("USD", menuItem.Price.Currency);
        Assert.Equal(20, menuItem.DisplayOrder);
        Assert.Equal(2, menuItem.Version);
        var domainEvent = Assert.IsType<MenuItemUpdatedDomainEvent>(
            Assert.Single(menuItem.DomainEvents));
        Assert.Equal(menuItem.Id, domainEvent.MenuItemId);
        Assert.Equal(menuItem.RestaurantId, domainEvent.RestaurantId);
        Assert.Equal(menuItem.CategoryId, domainEvent.CategoryId);
    }

    [Fact]
    public void UpdateShouldNormalizeBlankDescriptionToNull()
    {
        var menuItem = CreateMenuItem().Value;

        var result = menuItem.Update(
            menuItem.Name,
            "   ",
            menuItem.Price.Amount,
            menuItem.Price.Currency,
            menuItem.DisplayOrder);

        Assert.True(result.IsSuccess);
        Assert.Null(menuItem.Description);
        Assert.Equal(2, menuItem.Version);
    }

    [Fact]
    public void UpdateShouldNotChangeVersionOrRaiseEventForEquivalentValues()
    {
        var menuItem = CreateMenuItem().Value;
        menuItem.ClearDomainEvents();

        var result = menuItem.Update(
            " Item ",
            " Description ",
            10.00m,
            "eur",
            1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, menuItem.Version);
        Assert.Empty(menuItem.DomainEvents);
    }

    [Fact]
    public void UpdateShouldRejectInvalidMoneyWithoutChangingState()
    {
        var menuItem = CreateMenuItem().Value;
        menuItem.ClearDomainEvents();

        var result = menuItem.Update(
            "Changed",
            "Changed",
            -1m,
            "EUR",
            2);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuItemErrors.NegativePrice, result.Error);
        Assert.Equal("Item", menuItem.Name);
        Assert.Equal("Description", menuItem.Description);
        Assert.Equal(10m, menuItem.Price.Amount);
        Assert.Equal(1, menuItem.DisplayOrder);
        Assert.Equal(1, menuItem.Version);
        Assert.Empty(menuItem.DomainEvents);
    }

    [Fact]
    public void ChangeAvailabilityShouldIncrementVersionAndRaiseEvent()
    {
        var menuItem = CreateMenuItem().Value;
        menuItem.ClearDomainEvents();

        menuItem.ChangeAvailability(false);

        Assert.False(menuItem.IsAvailable);
        Assert.Equal(2, menuItem.Version);
        var domainEvent =
            Assert.IsType<MenuItemAvailabilityChangedDomainEvent>(
                Assert.Single(menuItem.DomainEvents));
        Assert.Equal(menuItem.Id, domainEvent.MenuItemId);
        Assert.Equal(menuItem.RestaurantId, domainEvent.RestaurantId);
        Assert.Equal(menuItem.CategoryId, domainEvent.CategoryId);
        Assert.False(domainEvent.IsAvailable);
    }

    [Fact]
    public void ChangeAvailabilityShouldBeNoOpForCurrentValue()
    {
        var menuItem = CreateMenuItem().Value;
        menuItem.ClearDomainEvents();

        menuItem.ChangeAvailability(true);

        Assert.True(menuItem.IsAvailable);
        Assert.Equal(1, menuItem.Version);
        Assert.Empty(menuItem.DomainEvents);
    }

    [Fact]
    public void DeleteShouldMarkItemIncrementVersionAndRaiseEvent()
    {
        var deletedAtUtc = CreatedAtUtc.AddHours(1);
        var menuItem = CreateMenuItem().Value;
        menuItem.ClearDomainEvents();

        menuItem.Delete(deletedAtUtc);

        Assert.True(menuItem.IsDeleted);
        Assert.Equal(deletedAtUtc, menuItem.DeletedAtUtc);
        Assert.Equal(2, menuItem.Version);
        var domainEvent = Assert.IsType<MenuItemDeletedDomainEvent>(
            Assert.Single(menuItem.DomainEvents));
        Assert.Equal(menuItem.Id, domainEvent.MenuItemId);
        Assert.Equal(menuItem.RestaurantId, domainEvent.RestaurantId);
        Assert.Equal(menuItem.CategoryId, domainEvent.CategoryId);
        Assert.Equal(deletedAtUtc, domainEvent.DeletedAtUtc);
    }

    [Fact]
    public void DeleteShouldBeIdempotentInsideAggregate()
    {
        var firstDeletedAtUtc = CreatedAtUtc.AddHours(1);
        var menuItem = CreateMenuItem().Value;
        menuItem.ClearDomainEvents();
        menuItem.Delete(firstDeletedAtUtc);

        menuItem.Delete(CreatedAtUtc.AddHours(2));

        Assert.Equal(firstDeletedAtUtc, menuItem.DeletedAtUtc);
        Assert.Equal(2, menuItem.Version);
        Assert.Single(menuItem.DomainEvents);
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
