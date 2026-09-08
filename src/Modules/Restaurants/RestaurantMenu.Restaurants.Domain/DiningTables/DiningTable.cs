using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.DiningTables;

public sealed class DiningTable : AggregateRoot<DiningTableId>
{
    public const int MaxDisplayNameLength = 80;

    private DiningTable(DiningTableId id, RestaurantId restaurantId, BranchId branchId,
        int number, string? displayName, short? capacity, DateTimeOffset createdAtUtc) : base(id)
    {
        RestaurantId = restaurantId;
        BranchId = branchId;
        Number = number;
        DisplayName = displayName;
        Capacity = capacity;
        CreatedAtUtc = createdAtUtc;
    }

    private DiningTable(DiningTableId id, RestaurantId restaurantId, BranchId branchId,
        int number, string? displayName, short? capacity, bool isActive,
        DateTimeOffset createdAtUtc, long version) : base(id)
    {
        RestaurantId = restaurantId; BranchId = branchId; Number = number;
        DisplayName = displayName; Capacity = capacity; IsActive = isActive;
        CreatedAtUtc = createdAtUtc; Version = version;
    }

    public RestaurantId RestaurantId { get; }
    public BranchId BranchId { get; }
    public int Number { get; private set; }
    public string? DisplayName { get; private set; }
    public short? Capacity { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset CreatedAtUtc { get; }
    public long Version { get; private set; } = 1;

    public static Result<DiningTable> Create(DiningTableId id, RestaurantId restaurantId,
        BranchId branchId, int number, string? displayName, short? capacity,
        DateTimeOffset createdAtUtc)
    {
        var details = Validate(number, displayName, capacity);
        if (details.IsFailure) return Result.Failure<DiningTable>(details.Error);
        var table = new DiningTable(id, restaurantId, branchId, number,
            details.Value.DisplayName, capacity, createdAtUtc);
        table.RaiseDomainEvent(new DiningTableCreatedDomainEvent(id, restaurantId, branchId));
        return Result.Success(table);
    }

    public Result<DiningTable> Update(int number, string? displayName, short? capacity)
    {
        var details = Validate(number, displayName, capacity);
        if (details.IsFailure) return Result.Failure<DiningTable>(details.Error);
        if (Number == number && DisplayName == details.Value.DisplayName && Capacity == capacity)
            return Result.Success(this);
        Number = number; DisplayName = details.Value.DisplayName; Capacity = capacity;
        Version++;
        RaiseDomainEvent(new DiningTableUpdatedDomainEvent(Id, RestaurantId, BranchId));
        return Result.Success(this);
    }

    public void ChangeStatus(bool isActive)
    {
        if (IsActive == isActive) return;
        IsActive = isActive;
        Version++;
        RaiseDomainEvent(new DiningTableStatusChangedDomainEvent(Id, RestaurantId, BranchId, isActive));
    }

    private static Result<TableDetails> Validate(int number, string? displayName, short? capacity)
    {
        if (number <= 0) return Result.Failure<TableDetails>(DiningTableErrors.InvalidNumber);
        displayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        if (displayName?.Length > MaxDisplayNameLength)
            return Result.Failure<TableDetails>(DiningTableErrors.DisplayNameTooLong);
        if (capacity <= 0) return Result.Failure<TableDetails>(DiningTableErrors.InvalidCapacity);
        return Result.Success(new TableDetails(displayName));
    }

    private sealed record TableDetails(string? DisplayName);
}
