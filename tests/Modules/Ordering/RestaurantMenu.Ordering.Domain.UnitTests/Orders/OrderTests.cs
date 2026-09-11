using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Domain.UnitTests.Orders;

public sealed class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldSnapshotTrustedLinesAndCalculateTotals()
    {
        var result = Order.Create(OrderId.New(), "O-123", Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), "Patio 4", Guid.NewGuid(), " no onions ",
            [new OrderLineSnapshot(Guid.NewGuid(), Guid.NewGuid(), " Burger ", " Large ", 12.50m, "eur", 2, " well done ")], Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(25.00m, result.Value.TotalAmount);
        Assert.Equal("EUR", result.Value.Currency);
        Assert.Equal("no onions", result.Value.CustomerNote);
        var line = Assert.Single(result.Value.Lines);
        Assert.Equal(25.00m, line.LineTotalAmount);
        Assert.Equal("Burger", line.ItemName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void CreateShouldRejectInvalidQuantity(int quantity)
    {
        var result = Create([Line(quantity)]);
        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidQuantity, result.Error);
    }

    [Fact]
    public void CreateShouldRejectEmptyAndOversizedOrders()
    {
        Assert.Equal(OrderErrors.LinesRequired, Create([]).Error);
        Assert.Equal(OrderErrors.TooManyLines,
            Create(Enumerable.Range(0, Order.MaxLineCount + 1).Select(_ => Line(1)).ToArray()).Error);
    }

    [Fact]
    public void CreateShouldRejectMixedCurrencies()
    {
        var result = Create([Line(1, "EUR"), Line(1, "USD")]);
        Assert.Equal(OrderErrors.MixedCurrencies, result.Error);
    }

    [Fact]
    public void CreateCalculatesDifferentInclusiveAndExclusiveItemTaxes()
    {
        var result = Create(
        [
            Line(1) with { UnitPriceAmount = 12.30m, TaxRateBasisPoints = 2_300,
                TaxBehavior = TaxBehavior.Inclusive },
            Line(1) with { UnitPriceAmount = 10m, TaxRateBasisPoints = 1_000,
                TaxBehavior = TaxBehavior.Exclusive }
        ]);

        Assert.True(result.IsSuccess);
        Assert.Equal(20m, result.Value.SubtotalAmount);
        Assert.Equal(3.30m, result.Value.TaxAmount);
        Assert.Equal(23.30m, result.Value.TotalAmount);
        Assert.Equal(2.30m, result.Value.Lines.First().TaxAmount);
        Assert.Equal(1m, result.Value.Lines.Last().TaxAmount);
    }

    private static RestaurantMenu.SharedKernel.Results.Result<Order> Create(IReadOnlyCollection<OrderLineSnapshot> lines) =>
        Order.Create(OrderId.New(), "O-123", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Table 1", Guid.NewGuid(), null, lines, Now);

    private static OrderLineSnapshot Line(int quantity, string currency = "EUR") =>
        new(Guid.NewGuid(), Guid.NewGuid(), "Burger", "Default", 10m, currency, quantity, null);
}
