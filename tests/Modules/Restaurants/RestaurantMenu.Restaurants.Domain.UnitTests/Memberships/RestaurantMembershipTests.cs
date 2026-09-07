using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.Memberships;

public sealed class RestaurantMembershipTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldNormalizeSubjectAndAssignRole()
    {
        var restaurantId = RestaurantId.New();

        var result = RestaurantMembership.Create(
            restaurantId,
            "  keycloak-user-123  ",
            RestaurantMembershipRole.Owner,
            CreatedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(restaurantId, result.Value.RestaurantId);
        Assert.Equal("keycloak-user-123", result.Value.Subject);
        Assert.Equal(RestaurantMembershipRole.Owner, result.Value.Role);
        Assert.Equal(CreatedAtUtc, result.Value.CreatedAtUtc);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateShouldRejectMissingSubject(string? subject)
    {
        var result = RestaurantMembership.Create(
            RestaurantId.New(),
            subject,
            RestaurantMembershipRole.Owner,
            CreatedAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(
            RestaurantMembershipErrors.SubjectRequired,
            result.Error);
    }

    [Fact]
    public void CreateShouldRejectEmptyRestaurantId()
    {
        var result = RestaurantMembership.Create(
            new RestaurantId(Guid.Empty),
            "keycloak-user-123",
            RestaurantMembershipRole.Owner,
            CreatedAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(
            RestaurantMembershipErrors.RestaurantRequired,
            result.Error);
    }

    [Fact]
    public void CreateShouldRejectInvalidRole()
    {
        var result = RestaurantMembership.Create(
            RestaurantId.New(),
            "keycloak-user-123",
            (RestaurantMembershipRole)999,
            CreatedAtUtc);

        Assert.True(result.IsFailure);
        Assert.Equal(
            RestaurantMembershipErrors.RoleInvalid,
            result.Error);
    }
}
