using RestaurantMenu.Catalog.Domain.Items;
using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Variants;

public sealed class MenuItemVariant : AggregateRoot<MenuItemVariantId>
{
    public const int MaxNameLength = 100;
    public const int MaxDescriptionLength = 500;

    private MenuItemVariant() : base(default)
    {
        Name = string.Empty;
        Price = null!;
    }

    private MenuItemVariant(
        MenuItemVariantId id,
        Guid restaurantId,
        MenuItemId menuItemId,
        string name,
        string? description,
        Money price,
        int displayOrder,
        bool isDefault,
        DateTimeOffset createdAtUtc) : base(id)
    {
        RestaurantId = restaurantId;
        MenuItemId = menuItemId;
        Name = name;
        Description = description;
        Price = price;
        DisplayOrder = displayOrder;
        IsDefault = isDefault;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid RestaurantId { get; }
    public MenuItemId MenuItemId { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Money Price { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsAvailable { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; }
    public long Version { get; private set; } = 1;
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<MenuItemVariant> Create(
        MenuItemVariantId id,
        Guid restaurantId,
        MenuItemId menuItemId,
        string? name,
        string? description,
        decimal priceAmount,
        string? currency,
        int displayOrder,
        bool isDefault,
        DateTimeOffset createdAtUtc)
    {
        if (restaurantId == Guid.Empty)
        {
            return Result.Failure<MenuItemVariant>(
                MenuItemVariantErrors.RestaurantRequired);
        }

        if (menuItemId.Value == Guid.Empty)
        {
            return Result.Failure<MenuItemVariant>(MenuItemVariantErrors.MenuItemRequired);
        }

        var values = Validate(name, description, priceAmount, currency, displayOrder);
        if (values.IsFailure)
        {
            return Result.Failure<MenuItemVariant>(values.Error);
        }

        var value = values.Value;
        var variant = new MenuItemVariant(
            id, restaurantId, menuItemId, value.Name, value.Description,
            value.Price, displayOrder, isDefault, createdAtUtc);
        variant.RaiseDomainEvent(new MenuItemVariantCreatedDomainEvent(
            id, restaurantId, menuItemId));
        return Result.Success(variant);
    }

    public Result<MenuItemVariant> Update(
        string? name,
        string? description,
        decimal priceAmount,
        string? currency,
        int displayOrder)
    {
        var values = Validate(name, description, priceAmount, currency, displayOrder);
        if (values.IsFailure)
        {
            return Result.Failure<MenuItemVariant>(values.Error);
        }

        var value = values.Value;
        if (Name == value.Name && Description == value.Description &&
            Price == value.Price && DisplayOrder == displayOrder)
        {
            return Result.Success(this);
        }

        Name = value.Name;
        Description = value.Description;
        Price = value.Price;
        DisplayOrder = displayOrder;
        Version++;
        RaiseDomainEvent(new MenuItemVariantUpdatedDomainEvent(
            Id, RestaurantId, MenuItemId));
        return Result.Success(this);
    }

    public void ChangeAvailability(bool isAvailable)
    {
        if (IsAvailable == isAvailable)
        {
            return;
        }

        IsAvailable = isAvailable;
        Version++;
        RaiseDomainEvent(new MenuItemVariantAvailabilityChangedDomainEvent(
            Id, RestaurantId, MenuItemId, isAvailable));
    }

    public void MakeDefault() => ChangeDefault(true);

    public void RemoveDefault() => ChangeDefault(false);

    public Result<MenuItemVariant> Delete(DateTimeOffset deletedAtUtc)
    {
        if (IsDefault)
        {
            return Result.Failure<MenuItemVariant>(
                MenuItemVariantErrors.DefaultCannotBeDeleted);
        }

        if (IsDeleted)
        {
            return Result.Success(this);
        }

        IsDeleted = true;
        DeletedAtUtc = deletedAtUtc;
        Version++;
        RaiseDomainEvent(new MenuItemVariantDeletedDomainEvent(
            Id, RestaurantId, MenuItemId, deletedAtUtc));
        return Result.Success(this);
    }

    private void ChangeDefault(bool isDefault)
    {
        if (IsDefault == isDefault)
        {
            return;
        }

        IsDefault = isDefault;
        Version++;
        RaiseDomainEvent(new MenuItemVariantDefaultChangedDomainEvent(
            Id, RestaurantId, MenuItemId, isDefault));
    }

    private static Result<ValidatedValues> Validate(
        string? name,
        string? description,
        decimal priceAmount,
        string? currency,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<ValidatedValues>(MenuItemVariantErrors.NameRequired);
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaxNameLength)
        {
            return Result.Failure<ValidatedValues>(MenuItemVariantErrors.NameTooLong);
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        if (normalizedDescription?.Length > MaxDescriptionLength)
        {
            return Result.Failure<ValidatedValues>(MenuItemVariantErrors.DescriptionTooLong);
        }

        if (displayOrder < 0)
        {
            return Result.Failure<ValidatedValues>(MenuItemVariantErrors.InvalidDisplayOrder);
        }

        var money = Money.Create(priceAmount, currency);
        return money.IsFailure
            ? Result.Failure<ValidatedValues>(money.Error)
            : Result.Success(new ValidatedValues(
                normalizedName, normalizedDescription, money.Value));
    }

    private sealed record ValidatedValues(
        string Name,
        string? Description,
        Money Price);
}
