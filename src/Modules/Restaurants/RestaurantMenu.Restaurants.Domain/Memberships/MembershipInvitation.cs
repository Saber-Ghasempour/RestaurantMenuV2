using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;
namespace RestaurantMenu.Restaurants.Domain.Memberships;

public readonly record struct MembershipInvitationId(Guid Value) { public static MembershipInvitationId New() => new(Guid.CreateVersion7()); }
public enum MembershipInvitationStatus { Pending = 1, Accepted = 2, Revoked = 3 }
public sealed class MembershipInvitation
{
    public const int MaxEmailLength = 320; public const int MaxTokenHashLength = 64;
    private MembershipInvitation() { Email = TokenHash = InvitedBySubject = string.Empty; }
    private MembershipInvitation(MembershipInvitationId id, RestaurantId restaurantId, string email, string tokenHash,
        RestaurantMembershipRole role, string invitedBySubject, DateTimeOffset created, DateTimeOffset expires)
    { Id = id; RestaurantId = restaurantId; Email = email; TokenHash = tokenHash; Role = role; InvitedBySubject = invitedBySubject; CreatedAtUtc = created; ExpiresAtUtc = expires; }
    public MembershipInvitationId Id { get; }
    public RestaurantId RestaurantId { get; }
    public string Email { get; }
    public string TokenHash { get; }
    public RestaurantMembershipRole Role { get; }
    public string InvitedBySubject { get; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset ExpiresAtUtc { get; }
    public MembershipInvitationStatus Status { get; private set; } = MembershipInvitationStatus.Pending;
    public string? AcceptedBySubject { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public DateTimeOffset? RevokedAtUtc { get; private set; }
    public long Version { get; private set; } = 1;
    public bool IsUsable(DateTimeOffset now) => Status == MembershipInvitationStatus.Pending && now < ExpiresAtUtc;
    public static Result<MembershipInvitation> Create(MembershipInvitationId id, RestaurantId restaurantId, string? email, string? hash, RestaurantMembershipRole role, string? actor, DateTimeOffset now, DateTimeOffset expires)
    { email = email?.Trim().ToLowerInvariant(); hash = hash?.Trim(); actor = actor?.Trim(); if (id.Value == Guid.Empty || restaurantId.Value == Guid.Empty || string.IsNullOrWhiteSpace(email) || email.Length > MaxEmailLength || !email.Contains('@') || string.IsNullOrWhiteSpace(hash) || hash.Length > MaxTokenHashLength || string.IsNullOrWhiteSpace(actor) || !Enum.IsDefined(role) || expires <= now) return Result.Failure<MembershipInvitation>(MembershipErrors.InvalidInvitation); return Result.Success<MembershipInvitation>(new(id, restaurantId, email, hash, role, actor, now, expires)); }
    public Result<MembershipInvitation> Accept(string subject, DateTimeOffset now) { if (!IsUsable(now)) return Result.Failure<MembershipInvitation>(MembershipErrors.InvitationUnavailable); Status = MembershipInvitationStatus.Accepted; AcceptedBySubject = subject; AcceptedAtUtc = now; Version++; return Result.Success(this); }
    public void Revoke(DateTimeOffset now) { if (Status != MembershipInvitationStatus.Pending) return; Status = MembershipInvitationStatus.Revoked; RevokedAtUtc = now; Version++; }
}
public static class MembershipErrors
{
    public static readonly ErrorDetail InvalidInvitation = ErrorDetail.Validation("Membership.InvalidInvitation", "Invitation data is invalid.");
    public static readonly ErrorDetail InvitationUnavailable = ErrorDetail.Conflict("Membership.InvitationUnavailable", "Invitation is expired, revoked, or already used.");
    public static readonly ErrorDetail DuplicateInvitation = ErrorDetail.Conflict("Membership.DuplicateInvitation", "An active invitation already exists for this email.");
    public static readonly ErrorDetail ForbiddenRole = ErrorDetail.Conflict("Membership.ForbiddenRole", "The requested role exceeds the actor's authority.");
    public static readonly ErrorDetail LastOwner = ErrorDetail.Conflict("Membership.LastOwner", "The final active owner cannot be changed, suspended, or revoked.");
    public static readonly ErrorDetail NotFound = ErrorDetail.NotFound("Membership.NotFound", "Membership or invitation was not found.");
    public static readonly ErrorDetail VersionConflict = ErrorDetail.Conflict("Membership.VersionConflict", "Membership version is stale.");
    public static readonly ErrorDetail IdentityProvisioningFailed = ErrorDetail.Failure("Membership.IdentityProvisioningFailed", "Identity provisioning is unavailable.");
}