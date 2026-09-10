using RestaurantMenu.Restaurants.Domain.Branches;
using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Application.Memberships;

public static class BranchMembershipApplicationErrors
{
    public static ErrorDetail BranchNotFound(BranchId id) => ErrorDetail.NotFound(
        "Restaurants.BranchNotFound", $"Branch '{id.Value}' was not found.");
    public static readonly ErrorDetail RestaurantMemberRequired = ErrorDetail.Validation(
        "Restaurants.RestaurantMemberRequired", "The subject must be a Restaurant member before Branch assignment.");
    public static readonly ErrorDetail VersionConflict = ErrorDetail.Conflict(
        "Restaurants.BranchMembershipVersionConflict", "The Branch membership changed since it was read.");
    public static readonly ErrorDetail ForbiddenRole = ErrorDetail.Conflict(
        "Restaurants.BranchMembershipRoleEscalation", "The Branch role exceeds the actor's authority.");
}