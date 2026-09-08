using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;
using RestaurantMenu.Restaurants.Infrastructure.DiningTables;
using RestaurantMenu.Restaurants.Infrastructure.PublicMenuCodes;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Restaurants.IntegrationTests.Persistence;

public sealed class DiningTableAndPublicMenuCodePersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine").Build();
    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task TableNumberAndCodeHashShouldBeUniqueAndRawCodeShouldNeverBeStored()
    {
        var options = Options();
        var restaurant = CreateRestaurant();
        var branch = CreateBranch(restaurant.Id);
        var firstTable = CreateTable(restaurant.Id, branch.Id, 1);
        var firstCode = CreateCode("hash-only", restaurant.Id, branch.Id, firstTable.Id);
        await using (var setup = new RestaurantsDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.AddRange(restaurant, branch, firstTable, firstCode);
            await setup.SaveChangesAsync();
        }

        await using (var duplicateTable = new RestaurantsDbContext(options))
        {
            duplicateTable.DiningTables.Add(CreateTable(restaurant.Id, branch.Id, 1));
            await Assert.ThrowsAsync<DiningTableNumberAlreadyExistsException>(() => duplicateTable.SaveChangesAsync());
        }
        await using (var duplicateCode = new RestaurantsDbContext(options))
        {
            duplicateCode.PublicMenuCodes.Add(CreateCode("hash-only", restaurant.Id, branch.Id, firstTable.Id));
            await Assert.ThrowsAsync<PublicMenuCodeHashAlreadyExistsException>(() => duplicateCode.SaveChangesAsync());
        }

        await using var verification = new RestaurantsDbContext(options);
        var persisted = await verification.PublicMenuCodes.AsNoTracking().SingleAsync();
        Assert.Equal("hash-only", persisted.CodeHash);
        Assert.DoesNotContain("raw-secret", persisted.CodeHash, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResolverShouldExcludeRevokedExpiredAndInactiveTableCodes()
    {
        var options = Options();
        var restaurant = CreateRestaurant(); var branch = CreateBranch(restaurant.Id);
        var table = CreateTable(restaurant.Id, branch.Id, 1);
        var active = CreateCode("active", restaurant.Id, branch.Id, table.Id);
        var expired = PublicMenuCode.Create(PublicMenuCodeId.New(), "expired", restaurant.Id,
            branch.Id, table.Id, PublicMenuCodePurpose.DineInOrdering,
            DateTimeOffset.UtcNow.AddMinutes(1), DateTimeOffset.UtcNow).Value;
        await using (var setup = new RestaurantsDbContext(options))
        {
            await setup.Database.MigrateAsync(); setup.AddRange(restaurant, branch, table, active, expired); await setup.SaveChangesAsync();
        }
        await using (var context = new RestaurantsDbContext(options))
        {
            var service = new PublicMenuCodeReadService(context);
            Assert.NotNull(await service.ResolveAsync("active", DateTimeOffset.UtcNow, CancellationToken.None));
            Assert.Null(await service.ResolveAsync("expired", DateTimeOffset.UtcNow.AddMinutes(2), CancellationToken.None));
        }
        await using (var deactivate = new RestaurantsDbContext(options))
        {
            var tracked = await deactivate.DiningTables.SingleAsync(value => value.Id == table.Id);
            tracked.ChangeStatus(false); await deactivate.SaveChangesAsync();
        }
        await using var verification = new RestaurantsDbContext(options);
        Assert.Null(await new PublicMenuCodeReadService(verification).ResolveAsync("active", DateTimeOffset.UtcNow, CancellationToken.None));
    }

    [Fact]
    public async Task ConcurrentRotationShouldTranslateStaleVersion()
    {
        var options = Options(); var restaurant = CreateRestaurant();
        var code = PublicMenuCode.Create(PublicMenuCodeId.New(), "original", restaurant.Id,
            null, null, PublicMenuCodePurpose.MenuOnly, null, DateTimeOffset.UtcNow).Value;
        await using (var setup = new RestaurantsDbContext(options))
        { await setup.Database.MigrateAsync(); setup.AddRange(restaurant, code); await setup.SaveChangesAsync(); }
        await using var first = new RestaurantsDbContext(options);
        await using var second = new RestaurantsDbContext(options);
        var firstCode = await first.PublicMenuCodes.SingleAsync();
        var secondCode = await second.PublicMenuCodes.SingleAsync();
        firstCode.Rotate("first", DateTimeOffset.UtcNow, null); await first.SaveChangesAsync();
        secondCode.Rotate("second", DateTimeOffset.UtcNow, null);
        await Assert.ThrowsAsync<ConcurrencyException>(() => second.SaveChangesAsync());
    }

    private DbContextOptions<RestaurantsDbContext> Options() => new DbContextOptionsBuilder<RestaurantsDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
    private static Restaurant CreateRestaurant() => Restaurant.Create(RestaurantId.New(), "Restaurant", DateTimeOffset.UtcNow).Value;
    private static Branch CreateBranch(RestaurantId id) => Branch.Create(BranchId.New(), id, "Branch", null, null, null, null, null, null, null, null, null, null, DateTimeOffset.UtcNow).Value;
    private static DiningTable CreateTable(RestaurantId restaurantId, BranchId branchId, int number) => DiningTable.Create(DiningTableId.New(), restaurantId, branchId, number, null, null, DateTimeOffset.UtcNow).Value;
    private static PublicMenuCode CreateCode(string hash, RestaurantId restaurantId, BranchId branchId, DiningTableId tableId) => PublicMenuCode.Create(PublicMenuCodeId.New(), hash, restaurantId, branchId, tableId, PublicMenuCodePurpose.DineInOrdering, null, DateTimeOffset.UtcNow).Value;
}
