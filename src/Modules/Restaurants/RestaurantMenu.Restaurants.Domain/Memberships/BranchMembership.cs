using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.Restaurants.Domain.Restaurants;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.Memberships;

public sealed class BranchMembership
{
    private BranchMembership(RestaurantId restaurantId, BranchId branchId, string subject,
        BranchMembershipRole role, BranchMembershipStatus status, DateTimeOffset createdAtUtc)
    {
        RestaurantId = restaurantId; BranchId = branchId; Subject = subject;
        Role = role; Status = status; CreatedAtUtc = createdAtUtc;
    }

    private BranchMembership(RestaurantId restaurantId, BranchId branchId, string subject,
        BranchMembershipRole role, BranchMembershipStatus status, DateTimeOffset createdAtUtc,
        long version) : this(restaurantId, branchId, subject, role, status, createdAtUtc)
    { Version = version; }

    public RestaurantId RestaurantId { get; }
    public BranchId BranchId { get; }
    public string Subject { get; }
    public BranchMembershipRole Role { get; private set; }
    public BranchMembershipStatus Status { get; private set; } = BranchMembershipStatus.Active;
    public DateTimeOffset CreatedAtUtc { get; }
    public long Version { get; private set; } = 1;

    public static Result<BranchMembership> Create(RestaurantId restaurantId, BranchId branchId,
        string? subject, BranchMembershipRole role, DateTimeOffset createdAtUtc,
        BranchMembershipStatus status = BranchMembershipStatus.Active)
    {
        if (restaurantId.Value == Guid.Empty || branchId.Value == Guid.Empty)
            return Result.Failure<BranchMembership>(BranchMembershipErrors.ScopeRequired);
        if (string.IsNullOrWhiteSpace(subject))
            return Result.Failure<BranchMembership>(BranchMembershipErrors.SubjectRequired);
        subject = subject.Trim();
        if (subject.Length > RestaurantMembership.MaxSubjectLength)
            return Result.Failure<BranchMembership>(BranchMembershipErrors.SubjectTooLong);
        if (!Enum.IsDefined(role))
            return Result.Failure<BranchMembership>(BranchMembershipErrors.RoleInvalid);
        if (!Enum.IsDefined(status))
            return Result.Failure<BranchMembership>(BranchMembershipErrors.StatusInvalid);
        return Result.Success(new BranchMembership(restaurantId, branchId, subject, role, status, createdAtUtc));
    }

    public Result<BranchMembership> Change(BranchMembershipRole role, BranchMembershipStatus status)
    {
        if (!Enum.IsDefined(role)) return Result.Failure<BranchMembership>(BranchMembershipErrors.RoleInvalid);
        if (!Enum.IsDefined(status)) return Result.Failure<BranchMembership>(BranchMembershipErrors.StatusInvalid);
        if (Role == role && Status == status) return Result.Success(this);
        Role = role; Status = status; Version++;
        return Result.Success(this);
    }
}
