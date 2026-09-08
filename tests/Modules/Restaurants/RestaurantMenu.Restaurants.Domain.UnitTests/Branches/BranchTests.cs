using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;

namespace RestaurantMenu.Restaurants.Domain.UnitTests.Branches;

public sealed class BranchTests
{
    private static readonly DateTimeOffset CreatedAtUtc =
        new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldNormalizeFieldsAndRaiseEvent()
    {
        var restaurantId = RestaurantId.New();

        var result = Branch.Create(
            BranchId.New(), restaurantId, " Downtown ", " Main-Branch ",
            " +351 210 000 000 ", " 1 Main Street ", " Lisbon ",
            " Lisbon ", " 1000-001 ", " pt ", 38.7223m, -9.1393m,
            " Europe/Lisbon ", CreatedAtUtc);

        Assert.True(result.IsSuccess);
        Assert.Equal(restaurantId, result.Value.RestaurantId);
        Assert.Equal("Downtown", result.Value.Name);
        Assert.Equal("main-branch", result.Value.Slug);
        Assert.Equal("PT", result.Value.CountryCode);
        Assert.True(result.Value.IsActive);
        Assert.Equal(1, result.Value.Version);
        Assert.IsType<BranchCreatedDomainEvent>(Assert.Single(result.Value.DomainEvents));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateShouldRejectMissingName(string? name)
    {
        var result = Create(name: name);

        Assert.True(result.IsFailure);
        Assert.Equal(BranchErrors.NameRequired, result.Error);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("-branch")]
    [InlineData("branch-")]
    [InlineData("branch--one")]
    [InlineData("branch one")]
    public void CreateShouldRejectInvalidSlug(string slug)
    {
        var result = Create(slug: slug);

        Assert.True(result.IsFailure);
        Assert.Equal(BranchErrors.InvalidSlug, result.Error);
    }

    [Fact]
    public void CreateShouldRequireCoordinatesTogether()
    {
        var result = Create(latitude: 38.7m, longitude: null);

        Assert.True(result.IsFailure);
        Assert.Equal(BranchErrors.CoordinatesMustBeProvidedTogether, result.Error);
    }

    [Theory]
    [InlineData("91", "0")]
    [InlineData("-91", "0")]
    [InlineData("0", "181")]
    [InlineData("0", "-181")]
    public void CreateShouldRejectOutOfRangeCoordinates(
        string latitude,
        string longitude)
    {
        var result = Create(
            latitude: decimal.Parse(latitude, System.Globalization.CultureInfo.InvariantCulture),
            longitude: decimal.Parse(longitude, System.Globalization.CultureInfo.InvariantCulture));

        Assert.True(result.IsFailure);
    }

    [Theory]
    [InlineData("P")]
    [InlineData("PRT")]
    [InlineData("P1")]
    public void CreateShouldRejectInvalidCountryCode(string countryCode)
    {
        var result = Create(countryCode: countryCode);

        Assert.True(result.IsFailure);
        Assert.Equal(BranchErrors.InvalidCountryCode, result.Error);
    }

    [Fact]
    public void UpdateShouldBeAtomicWhenAnyFieldIsInvalid()
    {
        var branch = Create().Value;
        branch.ClearDomainEvents();

        var result = branch.Update(
            "Changed", "invalid slug", null, null, null, null, null, null,
            null, null, null);

        Assert.True(result.IsFailure);
        Assert.Equal("Branch", branch.Name);
        Assert.Equal(1, branch.Version);
        Assert.Empty(branch.DomainEvents);
    }

    [Fact]
    public void NoOpUpdateAndStatusChangeShouldNotIncrementVersion()
    {
        var branch = Create().Value;
        branch.ClearDomainEvents();

        Assert.True(branch.Update(
            " Branch ", null, null, null, null, null, null, null,
            null, null, null).IsSuccess);
        branch.ChangeStatus(true);

        Assert.Equal(1, branch.Version);
        Assert.Empty(branch.DomainEvents);
    }

    [Fact]
    public void ChangeStatusShouldIncrementVersionAndRaiseEvent()
    {
        var branch = Create().Value;
        branch.ClearDomainEvents();

        branch.ChangeStatus(false);

        Assert.False(branch.IsActive);
        Assert.Equal(2, branch.Version);
        Assert.IsType<BranchStatusChangedDomainEvent>(Assert.Single(branch.DomainEvents));
    }

    [Fact]
    public void DeleteShouldBeIdempotent()
    {
        var branch = Create().Value;
        branch.ClearDomainEvents();
        var deletedAt = CreatedAtUtc.AddHours(1);

        branch.Delete(deletedAt);
        branch.Delete(deletedAt.AddHours(1));

        Assert.True(branch.IsDeleted);
        Assert.Equal(deletedAt, branch.DeletedAtUtc);
        Assert.Equal(2, branch.Version);
        Assert.Single(branch.DomainEvents);
    }

    private static RestaurantMenu.SharedKernel.Results.Result<Branch> Create(
        string? name = "Branch",
        string? slug = null,
        string? countryCode = null,
        decimal? latitude = null,
        decimal? longitude = null) =>
        Branch.Create(
            BranchId.New(), RestaurantId.New(), name, slug, null, null, null,
            null, null, countryCode, latitude, longitude, null, CreatedAtUtc);
}
