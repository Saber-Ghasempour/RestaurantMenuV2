using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Categories;

public sealed class MenuCategory
    : AggregateRoot<MenuCategoryId>
{
    public const int MaxNameLength = 100;

    private MenuCategory(
        MenuCategoryId id,
        Guid restaurantId,
        MenuCategoryId? parentId,
        string name,
        int displayOrder,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        RestaurantId = restaurantId;
        ParentId = parentId;
        Name = name;
        DisplayOrder = displayOrder;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RestaurantId { get; }

    public MenuCategoryId? ParentId { get; private set; }

    public string Name { get; private set; }

    public int DisplayOrder { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public long Version { get; private set; } = 1;

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<MenuCategory> Create(
        MenuCategoryId id,
        Guid restaurantId,
        MenuCategoryId? parentId,
        string? name,
        int displayOrder,
        DateTimeOffset createdAtUtc)
    {
        if (restaurantId == Guid.Empty)
        {
            return Result.Failure<MenuCategory>(
                MenuCategoryErrors.RestaurantRequired);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<MenuCategory>(
                MenuCategoryErrors.NameRequired);
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            return Result.Failure<MenuCategory>(
                MenuCategoryErrors.NameTooLong);
        }

        if (displayOrder < 0)
        {
            return Result.Failure<MenuCategory>(
                MenuCategoryErrors.InvalidDisplayOrder);
        }

        var category = new MenuCategory(
            id,
            restaurantId,
            parentId,
            normalizedName,
            displayOrder,
            createdAtUtc);

        category.RaiseDomainEvent(
            new MenuCategoryCreatedDomainEvent(
                id,
                restaurantId));

        return Result.Success(category);
    }

    public Result<MenuCategory> Update(
        MenuCategoryId? parentId,
        string? name,
        int displayOrder)
    {
        if (parentId == Id)
        {
            return Result.Failure<MenuCategory>(
                MenuCategoryErrors.CannotBeOwnParent);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<MenuCategory>(
                MenuCategoryErrors.NameRequired);
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            return Result.Failure<MenuCategory>(
                MenuCategoryErrors.NameTooLong);
        }

        if (displayOrder < 0)
        {
            return Result.Failure<MenuCategory>(
                MenuCategoryErrors.InvalidDisplayOrder);
        }

        if (ParentId == parentId &&
            Name == normalizedName &&
            DisplayOrder == displayOrder)
        {
            return Result.Success(this);
        }

        ParentId = parentId;
        Name = normalizedName;
        DisplayOrder = displayOrder;
        Version++;

        RaiseDomainEvent(
            new MenuCategoryUpdatedDomainEvent(
                Id,
                RestaurantId));

        return Result.Success(this);
    }

    public void Delete(DateTimeOffset deletedAtUtc)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        Version++;

        RaiseDomainEvent(
            new MenuCategoryDeletedDomainEvent(
                Id,
                RestaurantId,
                deletedAtUtc));
    }
}
