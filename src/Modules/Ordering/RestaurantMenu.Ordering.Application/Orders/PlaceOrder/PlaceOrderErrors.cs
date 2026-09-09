using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Application.Orders.PlaceOrder;

public static class PlaceOrderErrors
{
    public static readonly ErrorDetail IdempotencyKeyRequired = ErrorDetail.Validation("Ordering.IdempotencyKeyRequired", "A valid Idempotency-Key header is required.");
    public static readonly ErrorDetail IdempotencyKeyReused = ErrorDetail.Conflict("Ordering.IdempotencyKeyReused", "The idempotency key was already used with a different request.");
    public static readonly ErrorDetail ItemsNotOrderable = ErrorDetail.Conflict("Ordering.ItemsNotOrderable", "One or more requested items are not orderable for this branch.");
    public static readonly ErrorDetail DiningTableUnavailable = ErrorDetail.Conflict("Ordering.DiningTableUnavailable", "The dining table is no longer available.");
    public static readonly ErrorDetail IdempotencyRace = ErrorDetail.Conflict("Ordering.IdempotencyRequestInProgress", "An equivalent request is already being processed; retry with the same key.");
}
