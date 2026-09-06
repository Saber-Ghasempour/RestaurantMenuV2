using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application;

public static class CatalogApplicationErrors
{
    public static ErrorDetail RestaurantNotFound(
        Guid restaurantId) =>
        ErrorDetail.NotFound(
            "Catalog.RestaurantNotFound",
            $"Restaurant with identifier '{restaurantId}' was not found.");
}
