using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameStore.Users.Application.Common.Interfaces;
using GameStore.Users.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GameStore.Users.Infrastructure.Security;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string SecretKey { get; set; } = "GameStore_Super_Secret_Key_For_Jwt_Authentication_2026_Minimum_32_Chars";
    public string Issuer { get; set; } = "GameStoreAPI";
    public string Audience { get; set; } = "GameStoreClient";
    public int ExpirationHours { get; set; } = 8;
}

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _jwtOptions;

    public JwtTokenGenerator(IOptions<JwtOptions> jwtOptions)
    {
        _jwtOptions = jwtOptions.Value;
    }

    public string GenerateToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtOptions.SecretKey);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email.Value),
            new(JwtRegisteredClaimNames.Name, user.Name),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email.Value),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(_jwtOptions.ExpirationHours),
            Issuer = _jwtOptions.Issuer,
            Audience = _jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public class BcryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string plainPassword)
    {
        return BCrypt.Net.BCrypt.HashPassword(plainPassword, workFactor: 12);
    }

    public bool VerifyPassword(string plainPassword, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(plainPassword, passwordHash);
        }
        catch
        {
            return false;
        }
    }
}

