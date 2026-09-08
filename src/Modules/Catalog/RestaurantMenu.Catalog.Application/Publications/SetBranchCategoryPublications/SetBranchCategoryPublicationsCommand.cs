using RestaurantMenu.Application.Abstractions.Messaging;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Application.Publications.SetBranchCategoryPublications;

public sealed record SetBranchCategoryPublicationsCommand(
    Guid RestaurantId,
    Guid BranchId,
    IReadOnlyList<BranchCategoryPublicationInput> Publications)
    : ICommand<Result<bool>>;

public sealed record BranchCategoryPublicationInput(
    Guid CategoryId,
    bool IsPublished,
    int? DisplayOrderOverride);
