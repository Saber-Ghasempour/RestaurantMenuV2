using System.Security.Cryptography;
using System.Text;

using Microsoft.EntityFrameworkCore;

using RestaurantMenu.Restaurants.Application.Abstractions.Data;
using RestaurantMenu.Restaurants.Application.Abstractions.Security;
using RestaurantMenu.Restaurants.Domain.Memberships;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.Restaurants.Infrastructure.Database;
namespace RestaurantMenu.Restaurants.Infrastructure.Memberships;

public sealed class MembershipInvitationRepository(RestaurantsDbContext db) : IMembershipInvitationRepository
{ public void Add(MembershipInvitation invitation) => db.MembershipInvitations.Add(invitation); public void AddAudit(MembershipAuditEntry entry) => db.MembershipAuditEntries.Add(entry); public Task<MembershipInvitation?> GetByHashAsync(string hash, CancellationToken cancellationToken) => db.MembershipInvitations.SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken); public Task<MembershipInvitation?> GetPendingByEmailAsync(RestaurantId restaurantId, string email, CancellationToken cancellationToken) => db.MembershipInvitations.SingleOrDefaultAsync(x => x.RestaurantId == restaurantId && x.Email == email && x.Status == MembershipInvitationStatus.Pending, cancellationToken); public Task<MembershipInvitation?> GetAsync(RestaurantId restaurantId, MembershipInvitationId id, CancellationToken cancellationToken) => db.MembershipInvitations.SingleOrDefaultAsync(x => x.RestaurantId == restaurantId && x.Id == id, cancellationToken); public async Task<IReadOnlyList<MembershipInvitation>> ListAsync(RestaurantId restaurantId, CancellationToken cancellationToken) => await db.MembershipInvitations.AsNoTracking().Where(x => x.RestaurantId == restaurantId).OrderByDescending(x => x.CreatedAtUtc).ToArrayAsync(cancellationToken); public async Task<IReadOnlyList<MembershipAuditEntry>> ListAuditAsync(RestaurantId restaurantId, CancellationToken cancellationToken) => await db.MembershipAuditEntries.AsNoTracking().Where(x => x.RestaurantId == restaurantId).OrderByDescending(x => x.OccurredAtUtc).ToArrayAsync(cancellationToken); }
public sealed class CryptographicInvitationTokenGenerator : IInvitationTokenGenerator
{ public (string Raw, string Hash) Generate() { var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_'); return (raw, Hash(raw)); } public string Hash(string raw) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw))); public bool IsWellFormed(string raw) => raw.Length == 43 && raw.All(x => char.IsLetterOrDigit(x) || x is '-' or '_'); }
public sealed class UnconfiguredIdentityProvisioner : IIdentityProvisioner { public Task<string?> EnsureUserAsync(string email, CancellationToken cancellationToken) => Task.FromResult<string?>(null); }