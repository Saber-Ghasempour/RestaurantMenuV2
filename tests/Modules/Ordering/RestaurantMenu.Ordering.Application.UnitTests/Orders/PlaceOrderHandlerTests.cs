using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.DiningSessions.ResolveDiningSession;
using RestaurantMenu.Ordering.Application.Orders.PlaceOrder;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.UnitTests.Orders;

public sealed class PlaceOrderHandlerTests
{
    [Fact]
    public async Task ExpiredSessionShouldFailBeforeCatalogAccess()
    {
        var catalog = new CatalogProvider(null);
        var handler = new PlaceOrderCommandHandler(new SessionResolver(
            Result.Failure<DiningSessionScope>(DiningSessionErrors.InvalidCapability)), catalog,
            new TableProvider("1"), new FakeStore(), new FakeStore(), new FakeUnitOfWork(), TimeProvider.System);

        var result = await handler.Handle(new PlaceOrderCommand("expired", "key", null,
            [new PlaceOrderLine(Guid.NewGuid(), null, 1, null)]), CancellationToken.None);

        Assert.Equal(DiningSessionErrors.InvalidCapability, result.Error);
        Assert.Equal(0, catalog.CallCount);
    }

    [Fact]
    public async Task HandlerShouldUseCapabilityScopeAndTrustedSnapshots()
    {
        var session = new DiningSessionScope(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));
        var itemId = Guid.NewGuid(); var variantId = Guid.NewGuid();
        var repository = new FakeStore();
        var handler = new PlaceOrderCommandHandler(new SessionResolver(session),
            new CatalogProvider(new CatalogOrderLineSnapshot(itemId, variantId, "Server name", "Large", 7.25m, "EUR")),
            new TableProvider("Window 7"), repository, repository, new FakeUnitOfWork(), TimeProvider.System);

        var result = await handler.Handle(new PlaceOrderCommand("token", "key-1", "note",
            [new PlaceOrderLine(itemId, variantId, 2, null)]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(14.50m, result.Value.TotalAmount);
        Assert.Equal(session.RestaurantId, repository.Added!.RestaurantId);
        Assert.Equal(session.DiningTableId, repository.Added.DiningTableId);
        Assert.Equal("Window 7", repository.Added.TableDisplayName);
        Assert.Equal("Server name", repository.Added.Lines.Single().ItemName);
    }

    [Fact]
    public async Task SameKeyAndPayloadShouldReplayWhileChangedPayloadConflicts()
    {
        var session = new DiningSessionScope(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));
        var itemId = Guid.NewGuid(); var variantId = Guid.NewGuid(); var store = new FakeStore();
        var handler = new PlaceOrderCommandHandler(new SessionResolver(session),
            new CatalogProvider(new CatalogOrderLineSnapshot(itemId, variantId, "Tea", "Cup", 2m, "EUR")),
            new TableProvider("1"), store, store, new FakeUnitOfWork(), TimeProvider.System);
        var command = new PlaceOrderCommand("token", "same", null, [new PlaceOrderLine(itemId, variantId, 1, null)]);
        var first = await handler.Handle(command, CancellationToken.None);
        var replay = await handler.Handle(command, CancellationToken.None);
        var changed = await handler.Handle(command with { Lines = [new PlaceOrderLine(itemId, variantId, 2, null)] }, CancellationToken.None);

        Assert.Equal(first.Value.OrderId, replay.Value.OrderId);
        Assert.True(replay.Value.IsReplay);
        Assert.Equal(PlaceOrderErrors.IdempotencyKeyReused, changed.Error);
    }

    private sealed class SessionResolver : IQueryHandler<ResolveDiningSessionQuery, Result<DiningSessionScope>>
    {
        private readonly Result<DiningSessionScope> _result;
        public SessionResolver(DiningSessionScope scope) : this(Result.Success(scope)) { }
        public SessionResolver(Result<DiningSessionScope> result) => _result = result;
        public Task<Result<DiningSessionScope>> Handle(ResolveDiningSessionQuery query, CancellationToken ct) => Task.FromResult(_result);
    }
    private sealed class CatalogProvider(CatalogOrderLineSnapshot? snapshot) : ICatalogOrderSnapshotProvider
    {
        public int CallCount { get; private set; }
        public Task<IReadOnlyList<CatalogOrderLineSnapshot>?> GetOrderableAsync(Guid r, Guid b, IReadOnlyCollection<CatalogOrderLineRequest> l, CancellationToken ct)
        { CallCount++; return Task.FromResult<IReadOnlyList<CatalogOrderLineSnapshot>?>(snapshot is null ? null : [snapshot]); }
    }
    private sealed class TableProvider(string name) : IDiningTableSnapshotProvider
    { public Task<string?> GetDisplayNameAsync(Guid r, Guid b, Guid t, CancellationToken ct) => Task.FromResult<string?>(name); }
    private sealed class FakeUnitOfWork : IOrderingUnitOfWork
    { public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1); }
    private sealed class FakeStore : IOrderRepository, IIdempotencyRepository
    {
        private IdempotencyRecord? _record; public Order? Added { get; private set; }
        public void Add(Order order) => Added = order;
        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct) => Task.FromResult(Added?.Id == id ? Added : null);
        public Task<Order?> GetForUpdateAsync(Guid restaurantId, Guid branchId, OrderId id, CancellationToken ct) =>
            Task.FromResult(Added?.Id == id && Added.RestaurantId == restaurantId && Added.BranchId == branchId ? Added : null);
        public void Add(IdempotencyRecord record) => _record = record;
        public Task<IdempotencyRecord?> GetAsync(string scope, string key, DateTimeOffset now, CancellationToken ct) => Task.FromResult(_record is { } x && x.Scope == scope && x.Key == key ? x : null);
    }
}
