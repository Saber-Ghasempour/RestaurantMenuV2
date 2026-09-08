using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.DiningTables;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Domain;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.PublicMenuCodes;

public sealed class PublicMenuCode : AggregateRoot<PublicMenuCodeId>
{
    public const int MaxCodeHashLength = 128;

    private PublicMenuCode(PublicMenuCodeId id, string codeHash, RestaurantId restaurantId,
        BranchId? branchId, DiningTableId? diningTableId, PublicMenuCodePurpose purpose,
        DateTimeOffset? expiresAtUtc, DateTimeOffset createdAtUtc) : base(id)
    {
        CodeHash = codeHash; RestaurantId = restaurantId; BranchId = branchId;
        DiningTableId = diningTableId; Purpose = purpose; ExpiresAtUtc = expiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    private PublicMenuCode(PublicMenuCodeId id, string codeHash, RestaurantId restaurantId,
        BranchId? branchId, DiningTableId? diningTableId, PublicMenuCodePurpose purpose,
        bool isActive, DateTimeOffset? expiresAtUtc, DateTimeOffset createdAtUtc,
        DateTimeOffset? rotatedAtUtc, long version) : base(id)
    {
        CodeHash = codeHash; RestaurantId = restaurantId; BranchId = branchId;
        DiningTableId = diningTableId; Purpose = purpose; IsActive = isActive;
        ExpiresAtUtc = expiresAtUtc; CreatedAtUtc = createdAtUtc;
        RotatedAtUtc = rotatedAtUtc; Version = version;
    }

    public string CodeHash { get; private set; }
    public RestaurantId RestaurantId { get; }
    public BranchId? BranchId { get; }
    public DiningTableId? DiningTableId { get; }
    public PublicMenuCodePurpose Purpose { get; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset? ExpiresAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? RotatedAtUtc { get; private set; }
    public long Version { get; private set; } = 1;

    public static Result<PublicMenuCode> Create(PublicMenuCodeId id, string? codeHash,
        RestaurantId restaurantId, BranchId? branchId, DiningTableId? diningTableId,
        PublicMenuCodePurpose purpose, DateTimeOffset? expiresAtUtc, DateTimeOffset createdAtUtc)
    {
        var validation = Validate(codeHash, branchId, diningTableId, purpose, expiresAtUtc, createdAtUtc);
        if (validation.IsFailure) return Result.Failure<PublicMenuCode>(validation.Error);
        var code = new PublicMenuCode(id, codeHash!, restaurantId, branchId, diningTableId,
            purpose, expiresAtUtc, createdAtUtc);
        code.RaiseDomainEvent(new PublicMenuCodeCreatedDomainEvent(id, restaurantId));
        return Result.Success(code);
    }

    public Result<PublicMenuCode> Rotate(string? codeHash, DateTimeOffset rotatedAtUtc,
        DateTimeOffset? expiresAtUtc)
    {
        var validation = Validate(codeHash, BranchId, DiningTableId, Purpose, expiresAtUtc, rotatedAtUtc);
        if (validation.IsFailure) return Result.Failure<PublicMenuCode>(validation.Error);
        CodeHash = codeHash!; ExpiresAtUtc = expiresAtUtc; RotatedAtUtc = rotatedAtUtc;
        IsActive = true; Version++;
        RaiseDomainEvent(new PublicMenuCodeRotatedDomainEvent(Id, RestaurantId, rotatedAtUtc));
        return Result.Success(this);
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        if (!IsActive) return;
        IsActive = false; Version++;
        RaiseDomainEvent(new PublicMenuCodeRevokedDomainEvent(Id, RestaurantId, revokedAtUtc));
    }

    public bool IsResolvableAt(DateTimeOffset utcNow) => IsActive &&
        (!ExpiresAtUtc.HasValue || ExpiresAtUtc.Value > utcNow);

    private static Result<bool> Validate(string? codeHash, BranchId? branchId,
        DiningTableId? diningTableId, PublicMenuCodePurpose purpose,
        DateTimeOffset? expiresAtUtc, DateTimeOffset effectiveAtUtc)
    {
        if (string.IsNullOrWhiteSpace(codeHash) || codeHash.Length > MaxCodeHashLength)
            return Result.Failure<bool>(PublicMenuCodeErrors.InvalidCodeHash);
        var validScope = purpose switch
        {
            PublicMenuCodePurpose.MenuOnly => diningTableId is null,
            PublicMenuCodePurpose.DineInOrdering => branchId is not null && diningTableId is not null,
            _ => false
        };
        if (!validScope) return Result.Failure<bool>(PublicMenuCodeErrors.InvalidPurposeScope);
        if (expiresAtUtc <= effectiveAtUtc) return Result.Failure<bool>(PublicMenuCodeErrors.InvalidExpiry);
        return Result.Success(true);
    }
}
