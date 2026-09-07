namespace RestaurantMenu.Api.Authentication;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public required string MetadataAddress { get; init; }

    public required string ValidIssuer { get; init; }

    public required string Audience { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;
}
