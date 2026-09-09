namespace RestaurantMenu.Ordering.Application.Abstractions;

public enum PublicCodePurpose { MenuOnly = 1, DineInOrdering = 2 }
public sealed record PublicCodeScope(Guid RestaurantId, Guid? BranchId,
    Guid? DiningTableId, PublicCodePurpose Purpose);
public interface IPublicCodeResolver
{
    Task<PublicCodeScope?> ResolveAsync(string code, CancellationToken cancellationToken);
}
