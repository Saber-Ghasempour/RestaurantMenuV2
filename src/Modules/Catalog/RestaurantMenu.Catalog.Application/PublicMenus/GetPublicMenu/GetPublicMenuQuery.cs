using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.PublicMenus.GetPublicMenu;

public sealed record GetPublicMenuQuery(
    Guid RestaurantId)
    : IQuery<Result<PublicMenuResponse>>;
