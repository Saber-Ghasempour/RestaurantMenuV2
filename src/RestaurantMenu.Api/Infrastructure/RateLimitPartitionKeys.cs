using System.Security.Cryptography;
using System.Text;

namespace RestaurantMenu.Api.Infrastructure;

public static class RateLimitPartitionKeys
{
    public static string ForDiningSession(string? token, string fallback = "anonymous")
    {
        if (string.IsNullOrWhiteSpace(token)) return fallback;
        return $"session:{Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)))}";
    }

    public static string ReadDiningSessionPartition(HttpContext context)
    {
        var header = context.Request.Headers["X-Dining-Session"];
        var token = header.Count == 1 ? header[0] : null;
        if (string.IsNullOrWhiteSpace(token) &&
            context.Request.Cookies.TryGetValue("rm_dining_session", out var cookie)) token = cookie;
        return ForDiningSession(token, context.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }
}
