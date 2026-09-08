using RestaurantMenu.Restaurants.Domain.PublicMenuCodes;

namespace RestaurantMenu.Restaurants.Application.PublicMenuCodes;
public sealed record PublicMenuCodeResponse(Guid Id, Guid RestaurantId, Guid? BranchId,
    Guid? DiningTableId, PublicMenuCodePurpose Purpose, bool IsActive,
    DateTimeOffset? ExpiresAtUtc, DateTimeOffset CreatedAtUtc, DateTimeOffset? RotatedAtUtc,
    long Version);
public sealed record ResolvedPublicMenuCode(Guid RestaurantId, Guid? BranchId,
    Guid? DiningTableId, PublicMenuCodePurpose Purpose);
public sealed record IssuedPublicMenuCode(Guid Id, string Code, long Version);
