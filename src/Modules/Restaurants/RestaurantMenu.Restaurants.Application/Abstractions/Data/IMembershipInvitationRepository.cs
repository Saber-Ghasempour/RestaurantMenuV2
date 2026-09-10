using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;
namespace RestaurantMenu.Restaurants.Application.Abstractions.Data;

public interface IMembershipInvitationRepository
{ void Add(MembershipInvitation invitation); void AddAudit(MembershipAuditEntry entry); Task<MembershipInvitation?> GetByHashAsync(string hash, CancellationToken cancellationToken); Task<MembershipInvitation?> GetPendingByEmailAsync(RestaurantId restaurantId, string email, CancellationToken cancellationToken); Task<MembershipInvitation?> GetAsync(RestaurantId restaurantId, MembershipInvitationId id, CancellationToken cancellationToken); Task<IReadOnlyList<MembershipInvitation>> ListAsync(RestaurantId restaurantId, CancellationToken cancellationToken); Task<IReadOnlyList<MembershipAuditEntry>> ListAuditAsync(RestaurantId restaurantId, CancellationToken cancellationToken); }
public sealed class ActiveInvitationExistsException(string message, Exception inner) : Exception(message, inner);