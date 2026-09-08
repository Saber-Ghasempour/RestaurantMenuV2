using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Publications.GetBranchCatalogConfiguration;

public sealed record GetBranchCatalogConfigurationQuery(
    Guid RestaurantId,
    Guid BranchId)
    : IQuery<Result<IReadOnlyList<BranchCategoryPublicationResponse>>>;
