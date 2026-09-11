namespace RestaurantMenu.Payments.Domain.Payments;

public enum PaymentStatus
{
    Pending,
    Processing,
    Succeeded,
    Failed,
    Cancelled,
    PartiallyRefunded,
    Refunded
}
