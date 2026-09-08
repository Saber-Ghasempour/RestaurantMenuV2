using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Restaurants.Domain.Branches;

public sealed record BranchCreatedDomainEvent(
    BranchId BranchId,
    RestaurantId RestaurantId) : IDomainEvent;

public sealed record BranchUpdatedDomainEvent(
    BranchId BranchId,
    RestaurantId RestaurantId) : IDomainEvent;

public sealed record BranchStatusChangedDomainEvent(
    BranchId BranchId,
    RestaurantId RestaurantId,
    bool IsActive) : IDomainEvent;

public sealed record BranchDeletedDomainEvent(
    BranchId BranchId,
    RestaurantId RestaurantId,
    DateTimeOffset DeletedAtUtc) : IDomainEvent;
