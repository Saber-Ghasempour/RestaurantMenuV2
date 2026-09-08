using System.Security.Cryptography;
using System.Text;
using RestaurantMenu.Restaurants.Application.Abstractions.Security;

namespace RestaurantMenu.Restaurants.Infrastructure.PublicMenuCodes;
public sealed class CryptographicPublicMenuCodeGenerator : IPublicMenuCodeGenerator
{
    public string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public string Hash(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code))).ToLowerInvariant();
    }
}
