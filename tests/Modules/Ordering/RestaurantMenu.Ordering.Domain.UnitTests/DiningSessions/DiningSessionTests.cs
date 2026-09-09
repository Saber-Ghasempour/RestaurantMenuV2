using RestaurantMenu.Ordering.Domain.DiningSessions;

namespace RestaurantMenu.Ordering.Domain.UnitTests.DiningSessions;

public sealed class DiningSessionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldFixScopeAndRemainActiveBeforeExpiry()
    {
        var restaurantId = Guid.NewGuid(); var branchId = Guid.NewGuid(); var tableId = Guid.NewGuid();
        var result = DiningSession.Create(DiningSessionId.New(), new string('a', 64),
            restaurantId, branchId, tableId, Now, Now.AddHours(2));

        Assert.True(result.IsSuccess);
        Assert.Equal(restaurantId, result.Value.RestaurantId);
        Assert.Equal(branchId, result.Value.BranchId);
        Assert.Equal(tableId, result.Value.DiningTableId);
        Assert.True(result.Value.IsActiveAt(Now.AddHours(2).AddTicks(-1)));
        Assert.False(result.Value.IsActiveAt(Now.AddHours(2)));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateShouldRejectInvalidTokenHash(string hash)
    {
        var result = DiningSession.Create(DiningSessionId.New(), hash, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Now, Now.AddHours(2));
        Assert.True(result.IsFailure);
        Assert.Equal("DiningSession.InvalidTokenHash", result.Error.Code);
    }

    [Fact]
    public void CreateShouldRejectExpiryAtClockBoundary()
    {
        var result = DiningSession.Create(DiningSessionId.New(), new string('a', 64), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Now, Now);
        Assert.True(result.IsFailure);
        Assert.Equal("DiningSession.InvalidExpiry", result.Error.Code);
    }

    [Fact]
    public void RevokeShouldBeIdempotentAndImmediatelyInvalidateSession()
    {
        var session = DiningSession.Create(DiningSessionId.New(), new string('a', 64), Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), Now, Now.AddHours(2)).Value;
        session.Revoke(Now.AddMinutes(1));
        session.Revoke(Now.AddMinutes(2));
        Assert.Equal(Now.AddMinutes(1), session.RevokedAtUtc);
        Assert.False(session.IsActiveAt(Now.AddMinutes(1)));
    }
}
