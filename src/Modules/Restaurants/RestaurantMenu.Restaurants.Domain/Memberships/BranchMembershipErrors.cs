using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.Memberships;

public static class BranchMembershipErrors
{
    public static readonly ErrorDetail ScopeRequired = ErrorDetail.Validation("Restaurants.BranchMembershipScopeRequired", "Restaurant and Branch are required.");
    public static readonly ErrorDetail SubjectRequired = ErrorDetail.Validation("Restaurants.BranchMembershipSubjectRequired", "A subject is required.");
    public static readonly ErrorDetail SubjectTooLong = ErrorDetail.Validation("Restaurants.BranchMembershipSubjectTooLong", "The subject is too long.");
    public static readonly ErrorDetail RoleInvalid = ErrorDetail.Validation("Restaurants.BranchMembershipRoleInvalid", "The Branch role is invalid.");
    public static readonly ErrorDetail StatusInvalid = ErrorDetail.Validation("Restaurants.BranchMembershipStatusInvalid", "The Branch membership status is invalid.");
}
