using RestaurantMenu.Ordering.Domain.Orders;

namespace RestaurantMenu.Ordering.Domain.UnitTests.Orders;

public sealed class OrderStateMachineTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateShouldAppendPlacedHistory()
    {
        var order = Create();
        var history = Assert.Single(order.StatusHistory);
        Assert.Null(history.FromStatus);
        Assert.Equal(OrderStatus.Placed, history.ToStatus);
        Assert.Equal(OrderActorType.Guest, history.ChangedByType);
    }

    [Fact]
    public void HappyPathShouldApplyEveryMilestoneAndAppendHistory()
    {
        var order = Create();
        Assert.True(order.Accept("cashier", Now.AddMinutes(1)).IsSuccess);
        Assert.True(order.StartPreparing("cook", Now.AddMinutes(2)).IsSuccess);
        Assert.True(order.MarkReady("cook", Now.AddMinutes(3)).IsSuccess);
        Assert.True(order.MarkServed("waiter", Now.AddMinutes(4)).IsSuccess);
        Assert.True(order.Complete("cashier", Now.AddMinutes(5)).IsSuccess);

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(Now.AddMinutes(1), order.AcceptedAtUtc);
        Assert.Equal(Now.AddMinutes(5), order.CompletedAtUtc);
        Assert.Equal(6, order.StatusHistory.Count);
        Assert.Equal(6, order.Version);
    }

    [Theory]
    [InlineData(OrderStatus.Placed, OrderStatus.Accepted)]
    [InlineData(OrderStatus.Placed, OrderStatus.Rejected)]
    [InlineData(OrderStatus.Placed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Accepted, OrderStatus.Preparing)]
    [InlineData(OrderStatus.Accepted, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Preparing, OrderStatus.Ready)]
    [InlineData(OrderStatus.Preparing, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Ready, OrderStatus.Served)]
    [InlineData(OrderStatus.Served, OrderStatus.Completed)]
    public void DocumentedTransitionsShouldBeAllowed(OrderStatus from, OrderStatus to)
    {
        var order = At(from);
        Assert.True(Apply(order, to).IsSuccess);
        Assert.Equal(to, order.Status);
    }

    [Theory]
    [InlineData(OrderStatus.Accepted, OrderStatus.Rejected)]
    [InlineData(OrderStatus.Preparing, OrderStatus.Accepted)]
    [InlineData(OrderStatus.Ready, OrderStatus.Completed)]
    [InlineData(OrderStatus.Served, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Completed, OrderStatus.Accepted)]
    [InlineData(OrderStatus.Rejected, OrderStatus.Accepted)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Accepted)]
    public void ForbiddenAndTerminalTransitionsShouldFail(OrderStatus from, OrderStatus to)
    {
        var order = At(from); var version = order.Version; var historyCount = order.StatusHistory.Count;
        var result = Apply(order, to);
        Assert.Equal(OrderErrors.InvalidTransition(from, to), result.Error);
        Assert.Equal(version, order.Version);
        Assert.Equal(historyCount, order.StatusHistory.Count);
    }

    [Fact]
    public void RejectAndCancelShouldRequireBoundedReason()
    {
        Assert.Equal(OrderErrors.TransitionReasonRequired, Create().Reject("staff", " ", Now).Error);
        Assert.Equal(OrderErrors.TransitionReasonTooLong,
            Create().Cancel("staff", new string('x', OrderStatusHistory.MaxReasonLength + 1), Now).Error);
    }

    [Fact]
    public void SystemShouldCompleteServedOrderWithAttributedHistory()
    {
        var order = At(OrderStatus.Served);
        Assert.True(order.CompleteBySystem(Now).IsSuccess);
        var history = order.StatusHistory.Last();
        Assert.Equal(OrderActorType.System, history.ChangedByType);
        Assert.Null(history.ChangedBySubject);
    }

    private static Order At(OrderStatus status)
    {
        var order = Create();
        if (status == OrderStatus.Placed) return order;
        if (status == OrderStatus.Rejected) { order.Reject("staff", "reason", Now); return order; }
        if (status == OrderStatus.Cancelled) { order.Cancel("staff", "reason", Now); return order; }
        order.Accept("staff", Now);
        if (status == OrderStatus.Accepted) return order;
        order.StartPreparing("staff", Now);
        if (status == OrderStatus.Preparing) return order;
        order.MarkReady("staff", Now);
        if (status == OrderStatus.Ready) return order;
        order.MarkServed("staff", Now);
        if (status == OrderStatus.Served) return order;
        order.Complete("staff", Now); return order;
    }

    private static RestaurantMenu.SharedKernel.Results.Result<Order> Apply(Order order, OrderStatus target) => target switch
    {
        OrderStatus.Accepted => order.Accept("staff", Now),
        OrderStatus.Preparing => order.StartPreparing("staff", Now),
        OrderStatus.Ready => order.MarkReady("staff", Now),
        OrderStatus.Served => order.MarkServed("staff", Now),
        OrderStatus.Completed => order.Complete("staff", Now),
        OrderStatus.Rejected => order.Reject("staff", "reason", Now),
        OrderStatus.Cancelled => order.Cancel("staff", "reason", Now),
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };

    private static Order Create() => Order.Create(OrderId.New(), "O-STATE", Guid.NewGuid(),
        Guid.NewGuid(), Guid.NewGuid(), "Table", Guid.NewGuid(), null,
        [new OrderLineSnapshot(Guid.NewGuid(), Guid.NewGuid(), "Tea", "Cup", 2m, "EUR", 1, null)], Now).Value;
}
