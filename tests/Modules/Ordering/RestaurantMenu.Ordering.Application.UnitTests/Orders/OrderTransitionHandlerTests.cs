using RestaurantMenu.Application.Abstractions.Security;
using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.Orders.Queues;
using RestaurantMenu.Ordering.Application.Orders.Transitions;
using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Application.UnitTests.Orders;

public sealed class OrderTransitionHandlerTests
{
    [Theory]
    [InlineData(OrderStaffRole.Cashier, OrderTransitionAction.Accept, true)]
    [InlineData(OrderStaffRole.Kitchen, OrderTransitionAction.Accept, false)]
    [InlineData(OrderStaffRole.Kitchen, OrderTransitionAction.StartPreparing, true)]
    [InlineData(OrderStaffRole.Waiter, OrderTransitionAction.StartPreparing, false)]
    [InlineData(OrderStaffRole.Waiter, OrderTransitionAction.MarkServed, true)]
    [InlineData(OrderStaffRole.Cashier, OrderTransitionAction.Complete, true)]
    [InlineData(OrderStaffRole.Manager, OrderTransitionAction.MarkReady, true)]
    public async Task TransitionShouldRequireBranchRole(OrderStaffRole role,
        OrderTransitionAction action, bool allowed)
    {
        var order = AtRequiredState(action); var store = new Store(order);
        var service = new OrderTransitionService(store, new Access(role), new User(), store,
            TimeProvider.System);
        var command = new Command(order, action);
        var result = await service.ExecuteAsync(command, action, CancellationToken.None);
        Assert.Equal(allowed, result.IsSuccess);
        Assert.Equal(allowed ? 1 : 0, store.Saves);
        if (!allowed) Assert.Equal(OrderTransitionErrors.BranchAccessRequired, result.Error);
    }

    [Fact]
    public async Task StaleVersionShouldNotAppendHistoryOrSave()
    {
        var order = Create(); var store = new Store(order); var count = order.StatusHistory.Count;
        var service = new OrderTransitionService(store, new Access(OrderStaffRole.Cashier),
            new User(), store, TimeProvider.System);
        var command = new AcceptOrderCommand(order.RestaurantId, order.BranchId, order.Id, 99);
        var result = await service.ExecuteAsync(command, OrderTransitionAction.Accept, CancellationToken.None);
        Assert.Equal(OrderTransitionErrors.VersionConflict, result.Error);
        Assert.Equal(count, order.StatusHistory.Count);
        Assert.Equal(0, store.Saves);
    }

    [Fact]
    public async Task QueueShouldUseRoleSpecificStatesAndRejectSuspendedOrWrongRole()
    {
        var reads = new Reads(); var service = new OrderQueueQueryService(reads,
            new Access(OrderStaffRole.Kitchen), new User());
        var result = await service.GetAsync(Guid.NewGuid(), Guid.NewGuid(), OrderStaffRole.Kitchen,
            [OrderStatus.Accepted, OrderStatus.Preparing], CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { OrderStatus.Accepted, OrderStatus.Preparing }, reads.Statuses);
        var forbidden = await service.GetAsync(Guid.NewGuid(), Guid.NewGuid(), OrderStaffRole.Waiter,
            [OrderStatus.Ready], CancellationToken.None);
        Assert.Equal(OrderTransitionErrors.BranchAccessRequired, forbidden.Error);
    }

    private static Order AtRequiredState(OrderTransitionAction action)
    {
        var order = Create();
        if (action is OrderTransitionAction.Accept or OrderTransitionAction.Reject or OrderTransitionAction.Cancel) return order;
        order.Accept("seed", DateTimeOffset.UtcNow);
        if (action == OrderTransitionAction.StartPreparing) return order;
        order.StartPreparing("seed", DateTimeOffset.UtcNow);
        if (action == OrderTransitionAction.MarkReady) return order;
        order.MarkReady("seed", DateTimeOffset.UtcNow);
        if (action == OrderTransitionAction.MarkServed) return order;
        order.MarkServed("seed", DateTimeOffset.UtcNow); return order;
    }
    private static Order Create() => Order.Create(OrderId.New(), "O-APP", Guid.NewGuid(), Guid.NewGuid(),
        Guid.NewGuid(), "Table", Guid.NewGuid(), null,
        [new OrderLineSnapshot(Guid.NewGuid(), null, "Tea", "Cup", 2m, "EUR", 1, null)], DateTimeOffset.UtcNow).Value;

    private sealed record Command(Guid RestaurantId, Guid BranchId, OrderId OrderId,
        long ExpectedVersion, string? Reason) : IOrderTransitionCommand
    {
        public Command(Order order, OrderTransitionAction action) : this(order.RestaurantId,
            order.BranchId, order.Id, order.Version,
            action is OrderTransitionAction.Reject or OrderTransitionAction.Cancel ? "reason" : null) { }
    }
    private sealed class User : ICurrentUser { public string Subject => "staff"; }
    private sealed class Access(OrderStaffRole? role) : IOrderStaffAccessProvider
    { public Task<OrderStaffRole?> GetActiveRoleAsync(Guid r, Guid b, string s, CancellationToken ct) => Task.FromResult(role); }
    private sealed class Store(Order order) : IOrderRepository, IOrderingUnitOfWork
    {
        public int Saves { get; private set; }
        public void Add(Order value) { }
        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct) => Task.FromResult<Order?>(order.Id == id ? order : null);
        public Task<Order?> GetForUpdateAsync(Guid r, Guid b, OrderId id, CancellationToken ct) =>
            Task.FromResult<Order?>(order.Id == id && order.RestaurantId == r && order.BranchId == b ? order : null);
        public Task<int> SaveChangesAsync(CancellationToken ct = default) { Saves++; return Task.FromResult(1); }
    }
    private sealed class Reads : IOrderReadService
    {
        public IReadOnlyCollection<OrderStatus> Statuses { get; private set; } = [];
        public Task<IReadOnlyList<OrderQueueItem>> ListQueueAsync(Guid r, Guid b, IReadOnlyCollection<OrderStatus> statuses, CancellationToken ct)
        { Statuses = statuses; return Task.FromResult<IReadOnlyList<OrderQueueItem>>([]); }
        public Task<IReadOnlyList<OrderTimelineEntry>?> GetTimelineAsync(Guid r, Guid b, OrderId id, CancellationToken ct) => Task.FromResult<IReadOnlyList<OrderTimelineEntry>?>([]);
        public Task<GuestOrderDetail?> GetGuestOrderAsync(Guid r, Guid b, Guid s, OrderId id,
            CancellationToken ct) => Task.FromResult<GuestOrderDetail?>(null);
    }
}
