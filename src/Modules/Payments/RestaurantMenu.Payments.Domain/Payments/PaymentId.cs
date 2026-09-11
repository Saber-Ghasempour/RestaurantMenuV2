namespace RestaurantMenu.Payments.Domain.Payments;

public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId New() => new(Guid.CreateVersion7());
}
