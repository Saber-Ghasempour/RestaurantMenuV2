using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IMenuItemMediaRepository
{
    Task ReplaceAsync(Guid restaurantId, MenuItemId menuItemId, IReadOnlyCollection<MenuItemMedia> media, CancellationToken cancellationToken);
}
