using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Ordering.Domain.Orders;

public sealed class OrderLine : Entity<OrderLineId>
{
    public const int MaxItemNameLength = 150;
    public const int MaxVariantNameLength = 100;
    public const int MaxNoteLength = 500;
    public const int MaxQuantity = 99;

    private OrderLine() : base(default) { ItemName = string.Empty; Currency = string.Empty; }

    internal OrderLine(OrderLineId id, OrderId orderId, Guid menuItemId, Guid? variantId,
        string itemName, string? variantName, decimal unitPriceAmount, string currency,
        int quantity, decimal lineTotalAmount, string? note) : base(id)
    {
        OrderId = orderId; MenuItemId = menuItemId; VariantId = variantId;
        ItemName = itemName; VariantName = variantName; UnitPriceAmount = unitPriceAmount;
        Currency = currency; Quantity = quantity; LineTotalAmount = lineTotalAmount; Note = note;
    }

    public OrderId OrderId { get; }
    public Guid? MenuItemId { get; }
    public Guid? VariantId { get; }
    public string ItemName { get; }
    public string? VariantName { get; }
    public decimal UnitPriceAmount { get; }
    public string Currency { get; }
    public int Quantity { get; }
    public decimal LineTotalAmount { get; }
    public string? Note { get; }
}

public sealed record OrderLineSnapshot(Guid MenuItemId, Guid? VariantId, string ItemName,
    string? VariantName, decimal UnitPriceAmount, string Currency, int Quantity, string? Note);
