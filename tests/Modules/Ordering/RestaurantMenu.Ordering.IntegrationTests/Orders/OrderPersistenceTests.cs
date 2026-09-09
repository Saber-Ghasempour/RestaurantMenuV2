using Microsoft.EntityFrameworkCore;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.Ordering.Infrastructure.Database;
using Testcontainers.PostgreSql;

namespace RestaurantMenu.Ordering.IntegrationTests.Orders;

public sealed class OrderPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18.6-alpine").Build();
    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task MigrationShouldPersistOrderSnapshotsAndHaveNoPendingChanges()
    {
        var options = Options(); var sessionId = Guid.NewGuid(); var order = CreateOrder(sessionId, "O-PERSIST");
        await using (var context = new OrderingDbContext(options))
        {
            await context.Database.MigrateAsync();
            Assert.False(context.Database.HasPendingModelChanges());
            context.Orders.Add(order);
            context.IdempotencyRecords.Add(Record(order, sessionId, "persist"));
            await context.SaveChangesAsync();
        }
        await using var verify = new OrderingDbContext(options);
        var saved = await verify.Orders.AsNoTracking().Include(value => value.Lines).SingleAsync();
        Assert.Equal(21m, saved.TotalAmount);
        Assert.Equal("Table 4", saved.TableDisplayName);
        Assert.Equal(21m, Assert.Single(saved.Lines).LineTotalAmount);
        Assert.Single(await verify.IdempotencyRecords.AsNoTracking().ToArrayAsync());
    }

    [Fact]
    public async Task ConcurrentSameScopedKeyShouldPersistExactlyOneOrderAndAllowRetryLookup()
    {
        var options = Options(); var sessionId = Guid.NewGuid();
        await using (var migrate = new OrderingDbContext(options)) await migrate.Database.MigrateAsync();
        await using var first = new OrderingDbContext(options); await using var second = new OrderingDbContext(options);
        var firstOrder = CreateOrder(sessionId, "O-RACE-1"); var secondOrder = CreateOrder(sessionId, "O-RACE-2");
        first.Add(firstOrder); first.Add(Record(firstOrder, sessionId, "race"));
        second.Add(secondOrder); second.Add(Record(secondOrder, sessionId, "race"));
        var outcomes = await Task.WhenAll(SaveAsync(first), SaveAsync(second));
        Assert.Single(outcomes, succeeded => succeeded);
        Assert.Single(outcomes, succeeded => !succeeded);
        await using var verify = new OrderingDbContext(options);
        Assert.Equal(1, await verify.Orders.CountAsync());
        var record = await verify.IdempotencyRecords.SingleAsync();
        Assert.Contains(record.ResourceId, new[] { firstOrder.Id, secondOrder.Id });
    }

    private DbContextOptions<OrderingDbContext> Options() =>
        new DbContextOptionsBuilder<OrderingDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options;
    private static Order CreateOrder(Guid sessionId, string number) => Order.Create(OrderId.New(), number,
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Table 4", sessionId, null,
        [new OrderLineSnapshot(Guid.NewGuid(), Guid.NewGuid(), "Pasta", "Large", 10.50m, "EUR", 2, null)],
        DateTimeOffset.UtcNow).Value;
    private static IdempotencyRecord Record(Order order, Guid sessionId, string key) =>
        new(Guid.CreateVersion7(), $"dining-session:{sessionId:N}", key, new string('a', 64),
            order.Id, 201, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(24));

    private static async Task<bool> SaveAsync(OrderingDbContext context)
    {
        try { await context.SaveChangesAsync(); return true; }
        catch (IdempotencyKeyAlreadyExistsException) { return false; }
    }
}
