using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Template.Common.Services;
using Template.Common.Settings;

namespace Template.WebApi.Services;

/// <summary>
/// 產生包含使用者 Claims 的 JWT Token。
/// </summary>
public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    /// <summary>
    /// 建立 JWT 服務。
    /// </summary>
    /// <param name="jwtSettings">JWT 設定。</param>
    public JwtService(JwtSettings jwtSettings)
    {
        _jwtSettings = jwtSettings;
    }

    /// <inheritdoc />
    public string GenerateToken(string userId, string email, string mobilePhone, string deptId, string ip)
    {
        var now     = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(_jwtSettings.ExpiresMinutes);
        var tokenId = Guid.NewGuid().ToString();

        var claims = new[]
        {
            new Claim(ClaimTypes.Name,            userId),
            new Claim(ClaimTypes.Email,           email),
            new Claim(ClaimTypes.MobilePhone,     mobilePhone),
            new Claim("dept_id",                  deptId),
            new Claim("ip",                       ip),
            new Claim(JwtRegisteredClaimNames.Jti, tokenId),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(),     ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp, expires.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _jwtSettings.Issuer,
            audience:           _jwtSettings.Audience,
            claims:             claims,
            notBefore:          now.UtcDateTime,
            expires:            expires.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
