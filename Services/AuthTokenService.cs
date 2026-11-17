using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace PrisonManagementSystem.Services;

public static class AuthTokenService
{
    private static readonly byte[] SecretKey = SHA256.HashData(Encoding.UTF8.GetBytes("SindhPrisons-PMIS-Secure-Token-Key-2026!#$*"));

    public static string CreateToken(string username, string role, int staffId, string fullName, string rank)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var payload = $"{username}|{role}|{staffId}|{fullName}|{rank}|{timestamp}";
        using var hmac = new HMACSHA256(SecretKey);
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        var raw = $"{payload}|{hash}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static ClaimsPrincipal? ValidateToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = raw.Split('|');
            if (parts.Length != 7) return null;

            var username = parts[0];
            var role = parts[1];
            if (!int.TryParse(parts[2], out var staffId)) return null;
            var fullName = parts[3];
            var rank = parts[4];
            if (!long.TryParse(parts[5], out var timestamp)) return null;
            var expectedHash = parts[6];

            var payload = $"{username}|{role}|{staffId}|{fullName}|{rank}|{timestamp}";
            using var hmac = new HMACSHA256(SecretKey);
            var computedHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));

            if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expectedHash),
                Encoding.UTF8.GetBytes(computedHash)))
            {
                return null;
            }

            // Expiration check (24 hours)
            var tokenAge = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp;
            if (tokenAge > 86400 || tokenAge < -300) return null;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, staffId.ToString()),
                new(ClaimTypes.Name, username),
                new(ClaimTypes.GivenName, fullName),
                new(ClaimTypes.Role, role),
                new("StaffID", staffId.ToString()),
                new("OfficialRank", rank)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
        catch
        {
            return null;
        }
    }
}
