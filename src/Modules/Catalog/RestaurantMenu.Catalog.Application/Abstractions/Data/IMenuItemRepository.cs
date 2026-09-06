using RestaurantMenu.Catalog.Domain.Items;

namespace RestaurantMenu.Catalog.Application.Abstractions.Data;

public interface IMenuItemRepository
{
    void Add(MenuItem menuItem);
}
