using RestaurantMenu.Restaurants.Domain.Restaurants;
namespace RestaurantMenu.Restaurants.Domain.Memberships;

public sealed class MembershipAuditEntry
{
    private MembershipAuditEntry() { ActorSubject = Action = ResourceType = ResourceId = string.Empty; }
    public MembershipAuditEntry(Guid id, RestaurantId restaurantId, string actorSubject, string action,
        string resourceType, string resourceId, string? changes, DateTimeOffset occurredAtUtc)
    { Id = id; RestaurantId = restaurantId; ActorSubject = actorSubject; Action = action; ResourceType = resourceType; ResourceId = resourceId; Changes = changes; OccurredAtUtc = occurredAtUtc; }
    public Guid Id { get; }
    public RestaurantId RestaurantId { get; }
    public string ActorSubject { get; }
    public string Action { get; }
    public string ResourceType { get; }
    public string ResourceId { get; }
    public string? Changes { get; }
    public DateTimeOffset OccurredAtUtc { get; }
}