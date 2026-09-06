using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Domain.UnitTests.Items;

public sealed class MoneyTests
{
    [Fact]
    public void CreateShouldNormalizeCurrency()
    {
        var result = Money.Create(12.50m, " eur ");

        Assert.True(result.IsSuccess);
        Assert.Equal(12.50m, result.Value.Amount);
        Assert.Equal("EUR", result.Value.Currency);
    }

    [Fact]
    public void CreateShouldRejectNegativeAmount()
    {
        var result = Money.Create(-0.01m, "EUR");

        Assert.True(result.IsFailure);
        Assert.Equal(MenuItemErrors.NegativePrice, result.Error);
    }

    [Fact]
    public void CreateShouldRejectMoreThanTwoDecimalPlaces()
    {
        var result = Money.Create(1.999m, "EUR");

        Assert.True(result.IsFailure);
        Assert.Equal(
            MenuItemErrors.PricePrecisionExceeded,
            result.Error);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("12A")]
    public void CreateShouldRejectInvalidCurrency(string? currency)
    {
        var result = Money.Create(10m, currency);

        Assert.True(result.IsFailure);
    }
}
