using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.Catalog.Domain.Variants;

namespace RestaurantMenu.Catalog.Domain.UnitTests.Variants;

public sealed class MenuItemVariantTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldNormalizeValuesAndRaiseEvent()
    {
        var result = MenuItemVariant.Create(
            MenuItemVariantId.New(), Guid.CreateVersion7(), MenuItemId.New(),
            " Large ", " Serves two ", 19.90m, "eur", 2, true,
            CreatedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal("Large", result.Value.Name);
        Assert.Equal("Serves two", result.Value.Description);
        Assert.Equal(19.90m, result.Value.Price.Amount);
        Assert.Equal("EUR", result.Value.Price.Currency);
        Assert.True(result.Value.IsDefault);
        Assert.True(result.Value.IsAvailable);
        Assert.Equal(1, result.Value.Version);
        Assert.IsType<MenuItemVariantCreatedDomainEvent>(
            Assert.Single(result.Value.DomainEvents));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateShouldRejectMissingName(string? name)
    {
        var result = Create(name: name);

        Assert.True(result.IsFailure);
        Assert.Equal(MenuItemVariantErrors.NameRequired, result.Error);
    }

    [Fact]
    public void CreateShouldRejectInvalidMoneyAndOrdering()
    {
        Assert.Equal(MenuItemErrors.PricePrecisionExceeded,
            Create(amount: 1.999m).Error);
        Assert.Equal(MenuItemVariantErrors.InvalidDisplayOrder,
            Create(displayOrder: -1).Error);
    }

    [Fact]
    public void UpdateShouldBeAtomicAndIdempotent()
    {
        var variant = Create().Value;
        variant.ClearDomainEvents();

        var invalid = variant.Update("Changed", null, -1m, "EUR", 3);

        Assert.True(invalid.IsFailure);
        Assert.Equal("Default", variant.Name);
        Assert.Equal(1, variant.Version);
        Assert.Empty(variant.DomainEvents);

        var equivalent = variant.Update(
            " Default ", " Description ", 10m, "eur", 1);

        Assert.True(equivalent.IsSuccess);
        Assert.Equal(1, variant.Version);
        Assert.Empty(variant.DomainEvents);
    }

    [Fact]
    public void UpdateAndAvailabilityShouldIncrementVersion()
    {
        var variant = Create().Value;
        variant.ClearDomainEvents();

        Assert.True(variant.Update("Large", null, 12m, "EUR", 2).IsSuccess);
        Assert.Equal(2, variant.Version);
        Assert.IsType<MenuItemVariantUpdatedDomainEvent>(
            Assert.Single(variant.DomainEvents));

        variant.ClearDomainEvents();
        variant.ChangeAvailability(false);
        Assert.Equal(3, variant.Version);
        Assert.False(variant.IsAvailable);
        Assert.IsType<MenuItemVariantAvailabilityChangedDomainEvent>(
            Assert.Single(variant.DomainEvents));
    }

    [Fact]
    public void DefaultTransitionsShouldBeIdempotent()
    {
        var variant = Create(isDefault: false).Value;
        variant.ClearDomainEvents();

        variant.MakeDefault();
        variant.MakeDefault();

        Assert.True(variant.IsDefault);
        Assert.Equal(2, variant.Version);
        Assert.Single(variant.DomainEvents);

        variant.ClearDomainEvents();
        variant.RemoveDefault();
        Assert.False(variant.IsDefault);
        Assert.Equal(3, variant.Version);
    }

    [Fact]
    public void DeleteShouldRejectDefaultAndSoftDeleteNonDefault()
    {
        var defaultVariant = Create().Value;
        Assert.Equal(MenuItemVariantErrors.DefaultCannotBeDeleted,
            defaultVariant.Delete(CreatedAtUtc.AddHours(1)).Error);

        var variant = Create(isDefault: false).Value;
        var result = variant.Delete(CreatedAtUtc.AddHours(1));

        Assert.True(result.IsSuccess);
        Assert.True(variant.IsDeleted);
        Assert.Equal(2, variant.Version);
        Assert.IsType<MenuItemVariantDeletedDomainEvent>(
            Assert.Single(variant.DomainEvents.Skip(1)));
    }

    private static RestaurantMenu.SharedKernel.Results.Result<MenuItemVariant>
        Create(
            string? name = "Default",
            decimal amount = 10m,
            int displayOrder = 1,
            bool isDefault = true) =>
        MenuItemVariant.Create(
            MenuItemVariantId.New(), Guid.CreateVersion7(), MenuItemId.New(),
            name, "Description", amount, "EUR", displayOrder, isDefault,
            CreatedAtUtc);
}
