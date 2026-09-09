namespace RestaurantMenu.Catalog.Application.Publications;

public sealed record BranchCategoryPublicationResponse(
    Guid CategoryId,
    Guid? ParentCategoryId,
    string CategoryName,
    int CategoryDisplayOrder,
    bool IsPublished,
    int? DisplayOrderOverride,
    long? Version,
    bool IsCategoryPublished = false);
