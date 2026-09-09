using System.Security.Cryptography;
using System.Text;
using RestaurantMenu.Ordering.Application.Abstractions;

namespace RestaurantMenu.Ordering.Infrastructure.DiningSessions;
public sealed class CryptographicDiningSessionTokenGenerator : IDiningSessionTokenGenerator
{
    public string Generate() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    public string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    public bool IsWellFormed(string token) => token.Length == 43 && token.All(character =>
        char.IsAsciiLetterOrDigit(character) || character is '-' or '_');
}
