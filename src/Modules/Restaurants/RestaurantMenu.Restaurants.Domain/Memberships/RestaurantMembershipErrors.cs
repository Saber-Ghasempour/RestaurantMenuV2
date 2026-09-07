using RestaurantMenu.SharedKernel.Results;

namespace RestaurantMenu.Restaurants.Domain.Memberships;

public static class RestaurantMembershipErrors
{
    public static readonly ErrorDetail RestaurantRequired =
        ErrorDetail.Validation(
            "RestaurantMembership.RestaurantRequired",
            "A restaurant identifier is required.");

    public static readonly ErrorDetail SubjectRequired =
        ErrorDetail.Validation(
            "RestaurantMembership.SubjectRequired",
            "A user subject is required.");

    public static readonly ErrorDetail SubjectTooLong =
        ErrorDetail.Validation(
            "RestaurantMembership.SubjectTooLong",
            $"The user subject must not exceed {RestaurantMembership.MaxSubjectLength} characters.");

    public static readonly ErrorDetail RoleInvalid =
        ErrorDetail.Validation(
            "RestaurantMembership.RoleInvalid",
            "The restaurant membership role is invalid.");
}
