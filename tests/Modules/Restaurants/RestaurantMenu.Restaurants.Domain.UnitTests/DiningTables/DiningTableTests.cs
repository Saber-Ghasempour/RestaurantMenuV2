using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.DiningTables;

public sealed class DiningTableTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldNormalizeDetailsAndRaiseEvent()
    {
        var result = DiningTable.Create(DiningTableId.New(), RestaurantId.New(), BranchId.New(),
            7, " Terrace 7 ", 4, Now);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value.Number);
        Assert.Equal("Terrace 7", result.Value.DisplayName);
        Assert.Equal((short)4, result.Value.Capacity);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, result.Value.Version);
        Assert.IsType<DiningTableCreatedDomainEvent>(Assert.Single(result.Value.DomainEvents));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateShouldRejectNonPositiveNumber(int number)
    {
        var result = Create(number: number);

        Assert.True(result.IsFailure);
        Assert.Equal(DiningTableErrors.InvalidNumber, result.Error);
    }

    [Fact]
    public void CreateShouldRejectNonPositiveCapacity()
    {
        var result = Create(capacity: 0);

        Assert.True(result.IsFailure);
        Assert.Equal(DiningTableErrors.InvalidCapacity, result.Error);
    }

    [Fact]
    public void UpdateAndStatusChangesShouldBeVersionedAndNoOpsShouldNotBe()
    {
        var table = Create().Value;
        table.ClearDomainEvents();

        Assert.True(table.Update(2, " Window ", 6).IsSuccess);
        table.ChangeStatus(false);
        table.ChangeStatus(false);

        Assert.Equal(3, table.Version);
        Assert.False(table.IsActive);
        Assert.Collection(table.DomainEvents,
            item => Assert.IsType<DiningTableUpdatedDomainEvent>(item),
            item => Assert.IsType<DiningTableStatusChangedDomainEvent>(item));
    }

    private static RestaurantMenu.SharedKernel.Results.Result<DiningTable> Create(
        int number = 1, short? capacity = null) =>
        DiningTable.Create(DiningTableId.New(), RestaurantId.New(), BranchId.New(),
            number, null, capacity, Now);
}
