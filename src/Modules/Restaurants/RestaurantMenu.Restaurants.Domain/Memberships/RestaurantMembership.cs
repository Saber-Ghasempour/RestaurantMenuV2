using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.Memberships;

public sealed class RestaurantMembership
{
    public const int MaxSubjectLength = 255;

    private RestaurantMembership(
        RestaurantId restaurantId,
        string subject,
        RestaurantMembershipRole role,
        DateTimeOffset createdAtUtc)
    {
        RestaurantId = restaurantId;
        Subject = subject;
        Role = role;
        CreatedAtUtc = createdAtUtc;
    }

    private RestaurantMembership(RestaurantId restaurantId, string subject,
        RestaurantMembershipRole role, RestaurantMembershipStatus status,
        DateTimeOffset createdAtUtc, long version) : this(restaurantId, subject, role, createdAtUtc)
    { Status = status; Version = version; }

    public RestaurantId RestaurantId { get; }

    public string Subject { get; }

    public RestaurantMembershipRole Role { get; private set; }

    public RestaurantMembershipStatus Status { get; private set; } = RestaurantMembershipStatus.Active;

    public long Version { get; private set; } = 1;

    public DateTimeOffset CreatedAtUtc { get; }

    public static Result<RestaurantMembership> Create(
        RestaurantId restaurantId,
        string? subject,
        RestaurantMembershipRole role,
        DateTimeOffset createdAtUtc)
    {
        if (restaurantId.Value == Guid.Empty)
        {
            return Result.Failure<RestaurantMembership>(
                RestaurantMembershipErrors.RestaurantRequired);
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<RestaurantMembership>(
                RestaurantMembershipErrors.SubjectRequired);
        }

        var normalizedSubject = subject.Trim();

        if (normalizedSubject.Length > MaxSubjectLength)
        {
            return Result.Failure<RestaurantMembership>(
                RestaurantMembershipErrors.SubjectTooLong);
        }

        if (!Enum.IsDefined(role))
        {
            return Result.Failure<RestaurantMembership>(
                RestaurantMembershipErrors.RoleInvalid);
        }

        return Result.Success(
            new RestaurantMembership(
                restaurantId,
                normalizedSubject,
                role,
                createdAtUtc));
    }

    public Result<RestaurantMembership> Change(RestaurantMembershipRole role,
        RestaurantMembershipStatus status)
    {
        if (!Enum.IsDefined(role)) return Result.Failure<RestaurantMembership>(RestaurantMembershipErrors.RoleInvalid);
        if (!Enum.IsDefined(status)) return Result.Failure<RestaurantMembership>(RestaurantMembershipErrors.StatusInvalid);
        if (Role == role && Status == status) return Result.Success(this);
        Role = role; Status = status; Version++;
        return Result.Success(this);
    }
}

public enum RestaurantMembershipStatus { Active = 1, Suspended = 2, Revoked = 3 }