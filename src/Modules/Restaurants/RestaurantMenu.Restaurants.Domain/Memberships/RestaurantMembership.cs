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

    public RestaurantId RestaurantId { get; }

    public string Subject { get; }

    public RestaurantMembershipRole Role { get; private set; }

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
}
