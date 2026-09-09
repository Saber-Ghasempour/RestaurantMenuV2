using RestaurantMenu.Catalog.Domain.Categories;
using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Catalog.Domain.Items;

public sealed class MenuItem : AggregateRoot<MenuItemId>
{
    public const int MaxNameLength = 150;
    public const int MaxDescriptionLength = 1000;
    public const int MaxRecipeLength = 2000;
    public const int MaxAllergenNotesLength = 1000;
    public const int MaxTagCount = 20;
    public const int MaxTagLength = 50;
    public const int MaxPreparationTimeMinutes = 1440;

    private MenuItem()
        : base(default)
    {
        Name = string.Empty;
        Tags = [];
    }

    private MenuItem(
        MenuItemId id,
        Guid restaurantId,
        MenuCategoryId categoryId,
        string name,
        string? description,
        int displayOrder,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        RestaurantId = restaurantId;
        CategoryId = categoryId;
        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
        CreatedAtUtc = createdAtUtc;
        Tags = [];
    }

    public Guid RestaurantId { get; }

    public MenuCategoryId CategoryId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public int DisplayOrder { get; private set; }

    public bool IsAvailable { get; private set; } = true;

    public string? Recipe { get; private set; }

    public int? Calories { get; private set; }

    public string[] Tags { get; private set; }

    public string? AllergenNotes { get; private set; }

    public short? PreparationTimeMinutes { get; private set; }

    public bool IsFeatured { get; private set; }

    public bool IsPublished { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; }

    public long Version { get; private set; } = 1;

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Result<MenuItem> Create(
        MenuItemId id,
        Guid restaurantId,
        MenuCategoryId categoryId,
        string? name,
        string? description,
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

        var menuItem = new MenuItem(
            id,
            restaurantId,
            categoryId,
            normalizedName,
            normalizedDescription,
            displayOrder,
            createdAtUtc);

        menuItem.RaiseDomainEvent(
            new MenuItemCreatedDomainEvent(id, restaurantId));

        return Result.Success(menuItem);
    }

    public Result<MenuItem> Update(
        string? name,
        string? description,
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

        if (Name == normalizedName &&
            Description == normalizedDescription &&
            DisplayOrder == displayOrder)
        {
            return Result.Success(this);
        }

        Name = normalizedName;
        Description = normalizedDescription;
        DisplayOrder = displayOrder;
        Version++;

        RaiseDomainEvent(
            new MenuItemUpdatedDomainEvent(
                Id,
                RestaurantId,
                CategoryId));

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

        RaiseDomainEvent(
            new MenuItemAvailabilityChangedDomainEvent(
                Id,
                RestaurantId,
                CategoryId,
                isAvailable));
    }

    public void MarkDefaultVariantPriceUpdated()
    {
        Version++;
        RaiseDomainEvent(new MenuItemUpdatedDomainEvent(
            Id, RestaurantId, CategoryId));
    }

    public Result<MenuItem> UpdateMetadata(
        string? recipe,
        int? calories,
        IReadOnlyCollection<string>? tags,
        string? allergenNotes,
        int? preparationTimeMinutes,
        bool isFeatured)
    {
        var normalizedRecipe = NormalizeOptionalText(recipe);
        var normalizedAllergenNotes = NormalizeOptionalText(allergenNotes);
        if (normalizedRecipe?.Length > MaxRecipeLength)
        {
            return Result.Failure<MenuItem>(MenuItemErrors.RecipeTooLong);
        }

        if (calories < 0)
        {
            return Result.Failure<MenuItem>(MenuItemErrors.InvalidCalories);
        }

        if (normalizedAllergenNotes?.Length > MaxAllergenNotesLength)
        {
            return Result.Failure<MenuItem>(MenuItemErrors.AllergenNotesTooLong);
        }

        if (preparationTimeMinutes is <= 0 or > MaxPreparationTimeMinutes)
        {
            return Result.Failure<MenuItem>(MenuItemErrors.InvalidPreparationTime);
        }

        var normalizedTags = (tags ?? [])
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim().ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        if (normalizedTags.Length > MaxTagCount)
        {
            return Result.Failure<MenuItem>(MenuItemErrors.TooManyTags);
        }

        if (normalizedTags.Any(tag => tag.Length > MaxTagLength))
        {
            return Result.Failure<MenuItem>(MenuItemErrors.TagTooLong);
        }

        if (Recipe == normalizedRecipe && Calories == calories &&
            Tags.SequenceEqual(normalizedTags, StringComparer.Ordinal) &&
            AllergenNotes == normalizedAllergenNotes &&
            PreparationTimeMinutes == preparationTimeMinutes &&
            IsFeatured == isFeatured)
        {
            return Result.Success(this);
        }

        Recipe = normalizedRecipe;
        Calories = calories;
        Tags = normalizedTags;
        AllergenNotes = normalizedAllergenNotes;
        PreparationTimeMinutes = preparationTimeMinutes.HasValue
            ? checked((short)preparationTimeMinutes.Value)
            : null;
        IsFeatured = isFeatured;
        Version++;
        RaiseDomainEvent(new MenuItemMetadataUpdatedDomainEvent(Id, RestaurantId, CategoryId));
        return Result.Success(this);
    }

    public void ChangePublication(bool isPublished)
    {
        if (IsPublished == isPublished)
        {
            return;
        }

        IsPublished = isPublished;
        Version++;
        RaiseDomainEvent(new MenuItemPublicationChangedDomainEvent(
            Id, RestaurantId, CategoryId, isPublished));
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
            new MenuItemDeletedDomainEvent(
                Id,
                RestaurantId,
                CategoryId,
                deletedAtUtc));
    }
}
