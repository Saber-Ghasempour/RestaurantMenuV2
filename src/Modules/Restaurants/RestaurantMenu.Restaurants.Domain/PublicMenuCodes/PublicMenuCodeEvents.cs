using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Restaurants.Domain.PublicMenuCodes;

public sealed record PublicMenuCodeCreatedDomainEvent(PublicMenuCodeId PublicMenuCodeId, RestaurantId RestaurantId) : IDomainEvent;
public sealed record PublicMenuCodeRotatedDomainEvent(PublicMenuCodeId PublicMenuCodeId, RestaurantId RestaurantId, DateTimeOffset RotatedAtUtc) : IDomainEvent;
public sealed record PublicMenuCodeRevokedDomainEvent(PublicMenuCodeId PublicMenuCodeId, RestaurantId RestaurantId, DateTimeOffset RevokedAtUtc) : IDomainEvent;
