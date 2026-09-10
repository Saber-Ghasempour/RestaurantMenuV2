using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Infrastructure.Branches;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Restaurants.IntegrationTests.Persistence;

public sealed class BranchPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:18.6-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task RepositoryShouldPersistAndMaterializeBranch()
    {
        var options = CreateOptions();
        var restaurant = CreateRestaurant("Persistence Restaurant");
        var branch = CreateBranch(restaurant.Id, "Downtown", "downtown");

        await using (var context = new RestaurantsDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.Restaurants.Add(restaurant);
            new BranchRepository(context).Add(branch);
            await context.SaveChangesAsync();
        }

        await using var verification = new RestaurantsDbContext(options);
        var loaded = await new BranchRepository(verification).GetByIdAsync(
            restaurant.Id, branch.Id, CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal("Downtown", loaded.Name);
        Assert.Equal("downtown", loaded.Slug);
        Assert.Equal(1, loaded.Version);
        Assert.True(loaded.IsActive);
    }

    [Fact]
    public async Task RepositoryShouldEnforceRestaurantScopeAndSoftDeleteFilter()
    {
        var options = CreateOptions();
        var restaurant = CreateRestaurant("Scoped Restaurant");
        var branch = CreateBranch(restaurant.Id, "Scoped", null);

        await using (var context = new RestaurantsDbContext(options))
        {
            await context.Database.MigrateAsync();
            context.Restaurants.Add(restaurant);
            context.Branches.Add(branch);
            await context.SaveChangesAsync();
        }

        await using (var wrongScope = new RestaurantsDbContext(options))
        {
            Assert.Null(await new BranchRepository(wrongScope).GetByIdAsync(
                RestaurantId.New(), branch.Id, CancellationToken.None));
        }

        await using (var deletion = new RestaurantsDbContext(options))
        {
            var tracked = await deletion.Branches.SingleAsync(value => value.Id == branch.Id);
            tracked.Delete(DateTimeOffset.UtcNow);
            await deletion.SaveChangesAsync();
        }

        await using var verification = new RestaurantsDbContext(options);
        Assert.Null(await verification.Branches.SingleOrDefaultAsync(value => value.Id == branch.Id));
        Assert.NotNull(await verification.Branches.IgnoreQueryFilters()
            .SingleOrDefaultAsync(value => value.Id == branch.Id));
    }

    [Fact]
    public async Task SlugShouldBeUniqueInsideRestaurantButReusableAcrossRestaurants()
    {
        var options = CreateOptions();
        var firstRestaurant = CreateRestaurant("First Restaurant");
        var secondRestaurant = CreateRestaurant("Second Restaurant");

        await using (var setup = new RestaurantsDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Restaurants.AddRange(firstRestaurant, secondRestaurant);
            setup.Branches.AddRange(
                CreateBranch(firstRestaurant.Id, "First", "shared"),
                CreateBranch(secondRestaurant.Id, "Second", "shared"));
            await setup.SaveChangesAsync();
        }

        await using var duplicate = new RestaurantsDbContext(options);
        duplicate.Branches.Add(CreateBranch(firstRestaurant.Id, "Duplicate", "shared"));
        await Assert.ThrowsAsync<BranchSlugAlreadyExistsException>(
            () => duplicate.SaveChangesAsync());
    }

    [Fact]
    public async Task ReadServiceShouldFilterSearchStatusAndReturnDeterministicPage()
    {
        var options = CreateOptions();
        var restaurant = CreateRestaurant("Read Restaurant");
        var alpha = CreateBranch(restaurant.Id, "Alpha", null);
        var beta = CreateBranch(restaurant.Id, "Beta", null);
        beta.ChangeStatus(false);

        await using (var setup = new RestaurantsDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Restaurants.Add(restaurant);
            setup.Branches.AddRange(beta, alpha);
            await setup.SaveChangesAsync();
        }

        await using var context = new RestaurantsDbContext(options);
        var page = await new BranchReadService(context).GetPageAsync(
            restaurant.Id, 1, 10, "a", true, CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal(alpha.Id.Value, item.Id);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task BranchMembershipShouldRequireExactTenantRelationshipsAndRejectStaleUpdates()
    {
        var options = CreateOptions();
        var restaurant = CreateRestaurant("Membership Restaurant");
        var otherRestaurant = CreateRestaurant("Other Restaurant");
        var branch = CreateBranch(restaurant.Id, "Kitchen", null);
        const string subject = "staff-user";
        var restaurantMembership = RestaurantMembership.Create(restaurant.Id, subject,
            RestaurantMembershipRole.Staff, DateTimeOffset.UtcNow).Value;
        var branchMembership = BranchMembership.Create(restaurant.Id, branch.Id, subject,
            BranchMembershipRole.Kitchen, DateTimeOffset.UtcNow).Value;

        await using (var setup = new RestaurantsDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.Restaurants.AddRange(restaurant, otherRestaurant);
            setup.RestaurantMemberships.Add(restaurantMembership);
            setup.Branches.Add(branch);
            setup.BranchMemberships.Add(branchMembership);
            await setup.SaveChangesAsync();
        }

        await using var first = new RestaurantsDbContext(options);
        await using var second = new RestaurantsDbContext(options);
        var firstMembership = await first.BranchMemberships.SingleAsync();
        var secondMembership = await second.BranchMemberships.SingleAsync();
        firstMembership.Change(BranchMembershipRole.Manager, BranchMembershipStatus.Active);
        secondMembership.Change(BranchMembershipRole.Waiter, BranchMembershipStatus.Active);
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<ConcurrencyException>(() => second.SaveChangesAsync());

        await using var invalid = new RestaurantsDbContext(options);
        invalid.BranchMemberships.Add(BranchMembership.Create(otherRestaurant.Id, branch.Id, subject,
            BranchMembershipRole.Cashier, DateTimeOffset.UtcNow).Value);
        await Assert.ThrowsAsync<DbUpdateException>(() => invalid.SaveChangesAsync());
    }

    private DbContextOptions<RestaurantsDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<RestaurantsDbContext>()
            .UseNpgsql(_postgres.GetConnectionString()).Options;

    private static Restaurant CreateRestaurant(string name) =>
        Restaurant.Create(RestaurantId.New(), name, DateTimeOffset.UtcNow).Value;

    private static Branch CreateBranch(
        RestaurantId restaurantId,
        string name,
        string? slug) =>
        Branch.Create(
            BranchId.New(), restaurantId, name, slug, null, null, null, null,
            null, null, null, null, null, DateTimeOffset.UtcNow).Value;
}
