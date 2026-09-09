using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Ordering.Domain.Orders;

public sealed class Order : AggregateRoot<OrderId>
{
    public const int MaxLineCount = 50;
    public const int MaxPublicNumberLength = 24;
    public const int MaxTableDisplayNameLength = 80;
    public const int MaxCustomerNoteLength = 1000;
    private readonly List<OrderLine> _lines = [];

    private Order() : base(default) { PublicNumber = string.Empty; TableDisplayName = string.Empty; Currency = string.Empty; }
    private Order(OrderId id, string publicNumber, Guid restaurantId, Guid branchId,
        Guid diningTableId, string tableDisplayName, Guid diningSessionId, string? customerNote,
        string currency, decimal total, DateTimeOffset createdAtUtc, List<OrderLine> lines) : base(id)
    {
        PublicNumber = publicNumber; RestaurantId = restaurantId; BranchId = branchId;
        DiningTableId = diningTableId; TableDisplayName = tableDisplayName;
        DiningSessionId = diningSessionId; CustomerNote = customerNote; Currency = currency;
        SubtotalAmount = total; TotalAmount = total; CreatedAtUtc = createdAtUtc; _lines = lines;
    }

    public string PublicNumber { get; }
    public Guid RestaurantId { get; }
    public Guid BranchId { get; }
    public Guid DiningTableId { get; }
    public string TableDisplayName { get; }
    public Guid DiningSessionId { get; }
    public OrderStatus Status { get; private set; } = OrderStatus.Placed;
    public string Currency { get; }
    public decimal SubtotalAmount { get; }
    public decimal TotalAmount { get; }
    public string? CustomerNote { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public long Version { get; private set; } = 1;
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public static Result<Order> Create(OrderId id, string? publicNumber, Guid restaurantId,
        Guid branchId, Guid diningTableId, string? tableDisplayName, Guid diningSessionId,
        string? customerNote, IReadOnlyCollection<OrderLineSnapshot>? lines, DateTimeOffset createdAtUtc)
    {
        if (restaurantId == Guid.Empty || branchId == Guid.Empty || diningTableId == Guid.Empty || diningSessionId == Guid.Empty)
            return Result.Failure<Order>(OrderErrors.InvalidScope);
        publicNumber = publicNumber?.Trim();
        if (string.IsNullOrWhiteSpace(publicNumber) || publicNumber.Length > MaxPublicNumberLength)
            return Result.Failure<Order>(OrderErrors.InvalidPublicNumber);
        tableDisplayName = tableDisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(tableDisplayName) || tableDisplayName.Length > MaxTableDisplayNameLength)
            return Result.Failure<Order>(OrderErrors.TableNameRequired);
        customerNote = Normalize(customerNote);
        if (customerNote?.Length > MaxCustomerNoteLength)
            return Result.Failure<Order>(OrderErrors.CustomerNoteTooLong);
        if (lines is null || lines.Count == 0) return Result.Failure<Order>(OrderErrors.LinesRequired);
        if (lines.Count > MaxLineCount) return Result.Failure<Order>(OrderErrors.TooManyLines);

        var built = new List<OrderLine>(lines.Count); string? orderCurrency = null; decimal total = 0;
        foreach (var snapshot in lines)
        {
            if (snapshot.MenuItemId == Guid.Empty || snapshot.Quantity is < 1 or > OrderLine.MaxQuantity)
                return Result.Failure<Order>(OrderErrors.InvalidQuantity);
            var itemName = snapshot.ItemName?.Trim();
            if (string.IsNullOrWhiteSpace(itemName)) return Result.Failure<Order>(OrderErrors.ItemNameRequired);
            if (itemName.Length > OrderLine.MaxItemNameLength) return Result.Failure<Order>(OrderErrors.ItemNameTooLong);
            var variantName = Normalize(snapshot.VariantName);
            if (variantName?.Length > OrderLine.MaxVariantNameLength) return Result.Failure<Order>(OrderErrors.VariantNameTooLong);
            var note = Normalize(snapshot.Note);
            if (note?.Length > OrderLine.MaxNoteLength) return Result.Failure<Order>(OrderErrors.LineNoteTooLong);
            if (snapshot.UnitPriceAmount < 0 || decimal.Round(snapshot.UnitPriceAmount, 2) != snapshot.UnitPriceAmount)
                return Result.Failure<Order>(OrderErrors.InvalidPrice);
            var currency = snapshot.Currency?.Trim().ToUpperInvariant();
            if (currency is null || currency.Length != 3 || !currency.All(char.IsAsciiLetter))
                return Result.Failure<Order>(OrderErrors.InvalidCurrency);
            if (orderCurrency is not null && orderCurrency != currency)
                return Result.Failure<Order>(OrderErrors.MixedCurrencies);
            orderCurrency = currency;
            var lineTotal = checked(snapshot.UnitPriceAmount * snapshot.Quantity);
            total = checked(total + lineTotal);
            built.Add(new OrderLine(OrderLineId.New(), id, snapshot.MenuItemId, snapshot.VariantId,
                itemName, variantName, snapshot.UnitPriceAmount, currency, snapshot.Quantity, lineTotal, note));
        }
        var order = new Order(id, publicNumber, restaurantId, branchId, diningTableId,
            tableDisplayName, diningSessionId, customerNote, orderCurrency!, total, createdAtUtc, built);
        order.RaiseDomainEvent(new OrderPlacedDomainEvent(id, restaurantId, branchId, diningSessionId, total, orderCurrency!, createdAtUtc));
        return Result.Success(order);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
