namespace RestaurantMenu.Ordering.Application.Abstractions;

public sealed record CatalogOrderLineRequest(Guid MenuItemId, Guid? VariantId);
public sealed record CatalogOrderLineSnapshot(Guid MenuItemId, Guid? VariantId,
    string ItemName, string? VariantName, decimal UnitPriceAmount, string Currency);

public interface ICatalogOrderSnapshotProvider
{
    Task<IReadOnlyList<CatalogOrderLineSnapshot>?> GetOrderableAsync(Guid restaurantId,
        Guid branchId, IReadOnlyCollection<CatalogOrderLineRequest> lines,
        CancellationToken cancellationToken);
}

public interface IDiningTableSnapshotProvider
{
    Task<string?> GetDisplayNameAsync(Guid restaurantId, Guid branchId, Guid diningTableId,
        CancellationToken cancellationToken);
}
