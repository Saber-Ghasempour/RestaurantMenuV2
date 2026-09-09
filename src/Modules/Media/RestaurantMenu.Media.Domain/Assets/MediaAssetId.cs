namespace RestaurantMenu.Media.Domain.Assets;

public readonly record struct MediaAssetId(Guid Value)
{
    public static MediaAssetId New() => new(Guid.CreateVersion7());
}
