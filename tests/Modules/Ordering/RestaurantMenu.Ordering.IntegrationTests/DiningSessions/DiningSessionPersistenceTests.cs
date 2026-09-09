using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.Ordering.Infrastructure.Database;
using RestaurantMenu.Ordering.Infrastructure.DiningSessions;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Ordering.IntegrationTests.DiningSessions;
public sealed class DiningSessionPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine").Build();
    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public void TokenGeneratorShouldProvide256BitsAndStableLowercaseHash()
    {
        var generator = new CryptographicDiningSessionTokenGenerator();
        var tokens = Enumerable.Range(0, 100).Select(_ => generator.Generate()).ToArray();
        Assert.Equal(100, tokens.Distinct(StringComparer.Ordinal).Count());
        Assert.All(tokens, token => { Assert.Equal(43, token.Length); Assert.True(generator.IsWellFormed(token)); });
        Assert.All(tokens.Select(generator.Hash), hash =>
        { Assert.Equal(64, hash.Length); Assert.Equal(hash.ToLowerInvariant(), hash); });
    }

    [Fact]
    public async Task MigrationShouldEnforceHashUniquenessAndPersistOnlyHash()
    {
        var options = Options(); var now = DateTimeOffset.UtcNow;
        await using (var setup = new OrderingDbContext(options))
        {
            await setup.Database.MigrateAsync();
            setup.DiningSessions.Add(Create(new string('a', 64), now));
            await setup.SaveChangesAsync();
        }
        await using (var duplicate = new OrderingDbContext(options))
        {
            duplicate.DiningSessions.Add(Create(new string('a', 64), now));
            await Assert.ThrowsAsync<DiningSessionTokenHashAlreadyExistsException>(() => duplicate.SaveChangesAsync());
        }
        await using var verify = new OrderingDbContext(options);
        var persisted = await verify.DiningSessions.AsNoTracking().SingleAsync();
        Assert.Equal(new string('a', 64), persisted.TokenHash);
    }

    [Fact]
    public async Task ResolverShouldHonorExpiryRevocationAndExactScope()
    {
        var options = Options(); var now = DateTimeOffset.UtcNow;
        var active = Create("active-hash", now); var expired = Create("expired-hash", now.AddHours(-3));
        await using (var setup = new OrderingDbContext(options))
        { await setup.Database.MigrateAsync(); setup.AddRange(active, expired); await setup.SaveChangesAsync(); }
        await using (var query = new OrderingDbContext(options))
        {
            var repository = new DiningSessionRepository(query);
            var scope = await repository.ResolveAsync("active-hash", now, CancellationToken.None);
            Assert.NotNull(scope); Assert.Equal(active.DiningTableId, scope.DiningTableId);
            Assert.Null(await repository.ResolveAsync("expired-hash", now, CancellationToken.None));
        }
        await using (var revoke = new OrderingDbContext(options))
        { var tracked = await revoke.DiningSessions.SingleAsync(x => x.Id == active.Id); tracked.Revoke(now); await revoke.SaveChangesAsync(); }
        await using var verify = new OrderingDbContext(options);
        Assert.Null(await new DiningSessionRepository(verify).ResolveAsync("active-hash", now, CancellationToken.None));
    }

    private DbContextOptions<OrderingDbContext> Options() => new DbContextOptionsBuilder<OrderingDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
    private static DiningSession Create(string hash, DateTimeOffset created) => DiningSession.Create(
        DiningSessionId.New(), hash, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), created, created.AddHours(2)).Value;
}
