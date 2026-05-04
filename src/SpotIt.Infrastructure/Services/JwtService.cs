using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using SpotIt.Application.Interfaces;
using SpotIt.Domain.Entities;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace SpotIt.Infrastructure.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public string GenerateAccessToken(ApplicationUser user, IList<string> roles, IList<Claim> permissionClaims)
    {
        var claims=new List<Claim>
        {
             new(JwtRegisteredClaimNames.Sub, user.Id),
             new(JwtRegisteredClaimNames.Email, user.Email!),
             new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
             new("fullName", user.FullName),
             new("city", user.City)
        };
        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        foreach (var claim in permissionClaims)
            claims.Add(claim);

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));

        var credentials=new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                double.Parse(_configuration["Jwt:ExpiryMinutes"]!)),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);

    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng=RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey=true,
            IssuerSigningKey=new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!)),
            ValidateIssuer=true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateAudience=true,
            ValidAudience = _configuration["Jwt:Audience"],
            ValidateLifetime=false
        };

        var handler=new JwtSecurityTokenHandler();
        var principal = handler.ValidateToken(token, validationParameters, out var securityToken);
        if (securityToken is not JwtSecurityToken jwtToken ||
            !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256))
            return null;

        return principal;
    }
}
