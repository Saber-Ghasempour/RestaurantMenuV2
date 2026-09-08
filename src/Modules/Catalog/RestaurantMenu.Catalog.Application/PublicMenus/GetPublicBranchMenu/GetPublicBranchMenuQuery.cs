using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.PublicMenus.GetPublicBranchMenu;

public sealed record GetPublicBranchMenuQuery(
    Guid RestaurantId,
    Guid BranchId)
    : IQuery<Result<PublicBranchMenuResponse>>;
