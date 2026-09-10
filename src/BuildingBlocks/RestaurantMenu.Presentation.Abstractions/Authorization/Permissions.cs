namespace RestaurantMenu.Presentation.Abstractions.Authorization;

public static class Permissions
{
    public const string RestaurantsRead = "restaurants.read";

    public const string RestaurantsWrite = "restaurants.write";

    public const string BranchesRead = "branches.read";

    public const string BranchesWrite = "branches.write";

    public const string DiningTablesRead = "dining-tables.read";

    public const string DiningTablesWrite = "dining-tables.write";

    public const string PublicMenuCodesRead = "public-menu-codes.read";

    public const string PublicMenuCodesWrite = "public-menu-codes.write";

    public const string CatalogRead = "catalog.read";

    public const string CatalogWrite = "catalog.write";

    public const string MediaRead = "media.read";

    public const string MediaWrite = "media.write";

    public const string OrdersRead = "orders.read";
    public const string OrdersAccept = "orders.accept";
    public const string OrdersPrepare = "orders.prepare";
    public const string OrdersServe = "orders.serve";
    public const string OrdersComplete = "orders.complete";
    public const string OrdersCancel = "orders.cancel";

    public static readonly IReadOnlyList<string> All =
    [
        RestaurantsRead,
        RestaurantsWrite,
        BranchesRead,
        BranchesWrite,
        DiningTablesRead,
        DiningTablesWrite,
        PublicMenuCodesRead,
        PublicMenuCodesWrite,
        CatalogRead,
        CatalogWrite,
        MediaRead,
        MediaWrite,
        OrdersRead,
        OrdersAccept,
        OrdersPrepare,
        OrdersServe,
        OrdersComplete,
        OrdersCancel
    ];
}
