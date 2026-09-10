using RestaurantMenu.Ordering.Application.Abstractions;
using RestaurantMenu.Ordering.Application.Orders.GuestOrders;
using RestaurantMenu.Ordering.Domain.DiningSessions;
using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Application.UnitTests.Orders;

public sealed class GuestOrderHandlerTests
{
    [Fact]
    public async Task CancellationShouldRequireOwningCapabilityAndOpenWindow()
    {
        var now = DateTimeOffset.UtcNow;
        var order = Create(now, out var sessionId);
        var store = new Store(order);
        var valid = new CancelGuestOrderCommandHandler(new Sessions(order, sessionId), new Tokens(),
            store, store, new Clock(now.AddMinutes(5)), new GuestOrderOptions(TimeSpan.FromMinutes(5)));
        var result = await valid.Handle(new("token", order.Id, 1, "No longer dining"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(OrderActorType.Guest, order.StatusHistory.Last().ChangedByType);
        Assert.Equal(1, store.Saves);

        var otherOrder = Create(now, out var otherSessionId);
        var otherStore = new Store(otherOrder);
        var wrongSession = new CancelGuestOrderCommandHandler(new Sessions(otherOrder, Guid.NewGuid()),
            new Tokens(), otherStore, otherStore, new Clock(now),
            new GuestOrderOptions(TimeSpan.FromMinutes(5)));
        var denied = await wrongSession.Handle(new("token", otherOrder.Id, 1, "reason"), CancellationToken.None);
        Assert.Equal(DiningSessionErrors.InvalidCapability, denied.Error);
        Assert.Equal(0, otherStore.Saves);

        var lateOrder = Create(now, out var lateSessionId);
        var lateStore = new Store(lateOrder);
        var late = new CancelGuestOrderCommandHandler(new Sessions(lateOrder, lateSessionId),
            new Tokens(), lateStore, lateStore, new Clock(now.AddMinutes(5).AddTicks(1)),
            new GuestOrderOptions(TimeSpan.FromMinutes(5)));
        var closed = await late.Handle(new("token", lateOrder.Id, 1, "reason"), CancellationToken.None);
        Assert.Equal(GuestOrderErrors.CancellationWindowClosed, closed.Error);
        Assert.Equal(0, lateStore.Saves);
    }

    private static Order Create(DateTimeOffset createdAtUtc, out Guid sessionId)
    {
        sessionId = Guid.NewGuid();
        return Order.Create(OrderId.New(), "O-GUEST", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Table", sessionId, null,
            [new OrderLineSnapshot(Guid.NewGuid(), null, "Tea", null, 2m, "EUR", 1, null)],
            createdAtUtc).Value;
    }

    private sealed class Tokens : IDiningSessionTokenGenerator
    {
        public string Generate() => "token";
        public string Hash(string token) => "hash";
        public bool IsWellFormed(string token) => token == "token";
    }

    private sealed class Sessions(Order order, Guid sessionId) : IDiningSessionRepository
    {
        public void Add(DiningSession session) { }
        public Task<bool> TokenHashExistsAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<DiningSessionScope?> ResolveAsync(string tokenHash, DateTimeOffset utcNow,
            CancellationToken cancellationToken) => Task.FromResult<DiningSessionScope?>(new(sessionId,
                order.RestaurantId, order.BranchId, order.DiningTableId, utcNow.AddHours(1)));
    }

    private sealed class Store(Order order) : IOrderRepository, IOrderingUnitOfWork
    {
        public int Saves { get; private set; }
        public void Add(Order value) { }
        public Task<Order?> GetByIdAsync(OrderId id, CancellationToken cancellationToken) =>
            Task.FromResult<Order?>(id == order.Id ? order : null);
        public Task<Order?> GetForUpdateAsync(Guid restaurantId, Guid branchId, OrderId id,
            CancellationToken cancellationToken) => Task.FromResult<Order?>(id == order.Id &&
                restaurantId == order.RestaurantId && branchId == order.BranchId ? order : null);
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        { Saves++; return Task.FromResult(1); }
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    { public override DateTimeOffset GetUtcNow() => now; }
}
