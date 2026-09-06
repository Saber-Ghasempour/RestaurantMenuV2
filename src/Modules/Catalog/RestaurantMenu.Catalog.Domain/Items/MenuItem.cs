using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Items;

public sealed class MenuItem : AggregateRoot<MenuItemId>
{
    public const int MaxNameLength = 150;
    public const int MaxDescriptionLength = 1000;

    private MenuItem()
        : base(default)
    {
        Name = string.Empty;
        Price = null!;
    }

    private MenuItem(
        MenuItemId id,
        Guid restaurantId,
        MenuCategoryId categoryId,
        string name,
        string? description,
        Money price,
        int displayOrder,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        RestaurantId = restaurantId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        Price = price;
        DisplayOrder = displayOrder;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RestaurantId { get; }

    public MenuCategoryId CategoryId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public Money Price { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsAvailable { get; private set; } = true;

    public DateTimeOffset CreatedAtUtc { get; }

    public long Version { get; private set; } = 1;

    public static Result<MenuItem> Create(
        MenuItemId id,
        Guid restaurantId,
        MenuCategoryId categoryId,
        string? name,
        string? description,
        decimal priceAmount,
        string? currency,
        int displayOrder,
        DateTimeOffset createdAtUtc)
    {
        if (restaurantId == Guid.Empty)
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.RestaurantRequired);
        }

        if (categoryId.Value == Guid.Empty)
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.CategoryRequired);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.NameRequired);
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.NameTooLong);
        }

        var normalizedDescription =
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();

        if (normalizedDescription?.Length > MaxDescriptionLength)
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.DescriptionTooLong);
        }

        if (displayOrder < 0)
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.InvalidDisplayOrder);
        }

        var moneyResult = Money.Create(priceAmount, currency);

        if (moneyResult.IsFailure)
        {
            return Result.Failure<MenuItem>(moneyResult.Error);
        }

        var menuItem = new MenuItem(
            id,
            restaurantId,
            categoryId,
            normalizedName,
            normalizedDescription,
            moneyResult.Value,
            displayOrder,
            createdAtUtc);

        menuItem.RaiseDomainEvent(
            new MenuItemCreatedDomainEvent(id, restaurantId));

        return Result.Success(menuItem);
    }

    public Result<MenuItem> Update(
        string? name,
        string? description,
        decimal priceAmount,
        string? currency,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.NameRequired);
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.NameTooLong);
        }

        var normalizedDescription =
            string.IsNullOrWhiteSpace(description)
                ? null
                : description.Trim();

        if (normalizedDescription?.Length > MaxDescriptionLength)
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.DescriptionTooLong);
        }

        if (displayOrder < 0)
        {
            return Result.Failure<MenuItem>(
                MenuItemErrors.InvalidDisplayOrder);
        }

        var moneyResult = Money.Create(priceAmount, currency);

        if (moneyResult.IsFailure)
        {
            return Result.Failure<MenuItem>(moneyResult.Error);
        }

        if (Name == normalizedName &&
            Description == normalizedDescription &&
            Price == moneyResult.Value &&
            DisplayOrder == displayOrder)
        {
            return Result.Success(this);
        }

        Name = normalizedName;
        Description = normalizedDescription;
        Price = moneyResult.Value;
        DisplayOrder = displayOrder;
        Version++;

        RaiseDomainEvent(
            new MenuItemUpdatedDomainEvent(
                Id,
                RestaurantId,
                CategoryId));

        return Result.Success(this);
    }
}
