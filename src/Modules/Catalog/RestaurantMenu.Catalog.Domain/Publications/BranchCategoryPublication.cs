using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Publications;

public sealed class BranchCategoryPublication
    : AggregateRoot<BranchCategoryPublicationId>
{
    private BranchCategoryPublication()
        : base(default)
    {
    }

    private BranchCategoryPublication(
        BranchCategoryPublicationId id,
        Guid restaurantId,
        bool isPublished,
        int? displayOrderOverride,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        RestaurantId = restaurantId;
        BranchId = id.BranchId;
        CategoryId = id.CategoryId;
        IsPublished = isPublished;
        DisplayOrderOverride = displayOrderOverride;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RestaurantId { get; }

    public Guid BranchId { get; }

    public MenuCategoryId CategoryId { get; }

    public bool IsPublished { get; private set; }

    public int? DisplayOrderOverride { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public long Version { get; private set; } = 1;

    public static Result<BranchCategoryPublication> Create(
        Guid restaurantId,
        Guid branchId,
        MenuCategoryId categoryId,
        bool isPublished,
        int? displayOrderOverride,
        DateTimeOffset createdAtUtc)
    {
        if (restaurantId == Guid.Empty)
        {
            return Result.Failure<BranchCategoryPublication>(
                BranchCategoryPublicationErrors.RestaurantRequired);
        }

        if (branchId == Guid.Empty)
        {
            return Result.Failure<BranchCategoryPublication>(
                BranchCategoryPublicationErrors.BranchRequired);
        }

        if (categoryId.Value == Guid.Empty)
        {
            return Result.Failure<BranchCategoryPublication>(
                BranchCategoryPublicationErrors.CategoryRequired);
        }

        if (displayOrderOverride < 0)
        {
            return Result.Failure<BranchCategoryPublication>(
                BranchCategoryPublicationErrors.InvalidDisplayOrder);
        }

        return Result.Success(
            new BranchCategoryPublication(
                new BranchCategoryPublicationId(branchId, categoryId),
                restaurantId,
                isPublished,
                displayOrderOverride,
                createdAtUtc));
    }

    public Result<BranchCategoryPublication> Set(
        bool isPublished,
        int? displayOrderOverride)
    {
        if (displayOrderOverride < 0)
        {
            return Result.Failure<BranchCategoryPublication>(
                BranchCategoryPublicationErrors.InvalidDisplayOrder);
        }

        if (IsPublished == isPublished &&
            DisplayOrderOverride == displayOrderOverride)
        {
            return Result.Success(this);
        }

        IsPublished = isPublished;
        DisplayOrderOverride = displayOrderOverride;
        Version++;
        RaiseDomainEvent(
            new BranchCategoryPublicationChangedDomainEvent(
                RestaurantId,
                BranchId,
                CategoryId));

        return Result.Success(this);
    }
}
