namespace RestaurantMenu.Presentation.Abstractions.Authorization;

public static class Permissions
{
    public const string RestaurantsRead = "restaurants.read";

    public const string RestaurantsWrite = "restaurants.write";

    public const string BranchesRead = "branches.read";

    public const string BranchesWrite = "branches.write";

    public const string CatalogRead = "catalog.read";

    public const string CatalogWrite = "catalog.write";

    public static readonly IReadOnlyList<string> All =
    [
        RestaurantsRead,
        RestaurantsWrite,
        BranchesRead,
        BranchesWrite,
        CatalogRead,
        CatalogWrite
    ];
}
