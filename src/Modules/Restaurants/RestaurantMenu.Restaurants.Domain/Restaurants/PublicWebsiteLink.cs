namespace RestaurantMenu.Restaurants.Domain.Restaurants;

internal static class PublicWebsiteLink
{
    public const int MaxLength = 2048;

    public static bool IsValid(string? value)
    {
        if (value is null)
        {
            return true;
        }

        return value.Length <= MaxLength &&
            !value.Any(char.IsWhiteSpace) &&
            !value.Any(char.IsControl) &&
            !value.Contains('\\') &&
            Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            uri.Scheme == Uri.UriSchemeHttps &&
            !string.IsNullOrEmpty(uri.Host) &&
            string.IsNullOrEmpty(uri.UserInfo);
    }
}
