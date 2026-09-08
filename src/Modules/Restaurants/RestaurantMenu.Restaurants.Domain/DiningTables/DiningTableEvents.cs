using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Domain;

namespace RestaurantMenu.Restaurants.Domain.DiningTables;

public sealed record DiningTableCreatedDomainEvent(DiningTableId DiningTableId, RestaurantId RestaurantId, BranchId BranchId) : IDomainEvent;
public sealed record DiningTableUpdatedDomainEvent(DiningTableId DiningTableId, RestaurantId RestaurantId, BranchId BranchId) : IDomainEvent;
public sealed record DiningTableStatusChangedDomainEvent(DiningTableId DiningTableId, RestaurantId RestaurantId, BranchId BranchId, bool IsActive) : IDomainEvent;
