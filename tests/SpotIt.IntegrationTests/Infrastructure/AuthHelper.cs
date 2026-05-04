using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SpotIt.IntegrationTests.Infrastructure;

public static class AuthHelper
{
    private const string SecretKey = "test-secret-key-must-be-at-least-32-chars-long";

    public static string GenerateToken(string userId, string email, string role, params string[] permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", "Test User"),
            new("city", "Test City"),
            new(ClaimTypes.Role, role)
        };

        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "SpotIt.API",
            audience: "SpotIt.Client",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static HttpClient AsRole(this HttpClient client, string userId, string email, string role, params string[] permissions)
    {
        var token = GenerateToken(userId, email, role, permissions);
        client.DefaultRequestHeaders.Add("Cookie", $"accessToken={token}");
        return client;
    }
}
