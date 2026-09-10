using RestaurantMenu.Ordering.Domain.Orders;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.Transitions;

public static class OrderTransitionErrors
{
    public static ErrorDetail NotFound(OrderId id) => ErrorDetail.NotFound(
        "Ordering.OrderNotFound", $"Order '{id.Value}' was not found.");
    public static readonly ErrorDetail VersionConflict = ErrorDetail.Conflict(
        "Ordering.OrderVersionConflict", "The order changed since it was read.");
    public static readonly ErrorDetail BranchAccessRequired = new(
        "Ordering.BranchAccessRequired", "Active Branch membership with the required role is required.", ErrorType.Forbidden);
}
