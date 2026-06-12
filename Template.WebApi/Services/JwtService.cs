using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Template.BusinessRule.TokenRevocationService.Services;
using Template.Common.Enums;
using Template.Common.Models.Jwt;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.WebApi.Services;

/// <summary>
/// 提供 JWT 產生、驗證與設定管理功能。
/// </summary>
public class JwtService(
    ProjectDbContext db,
    ITokenRevocationService tokenRevocationService,
    IConfiguration configuration) : IJwtService
{
    private static readonly string JwtSettingType = JwtSettingTypeEnum.JwtSetting.ToSettingTypeValue();
    private static readonly string PersonalTokenExpire = JwtSettingKeyEnum.PersonalTokenExpire.ToSettingKeyValue();
    private static readonly string ServerTokenExpire = JwtSettingKeyEnum.ServerTokenExpire.ToSettingKeyValue();
    private const string TokenTypePersonal = "personal";
    private const string TokenTypeServer = "server";

    /// <summary>
    /// 產生個人使用者 JWT。
    /// </summary>
    /// <param name="userId">使用者識別碼。</param>
    /// <param name="email">使用者電子郵件。</param>
    /// <param name="mobilePhone">使用者手機號碼。</param>
    /// <param name="deptId">使用者部門識別碼。</param>
    /// <param name="ip">請求來源 IP。</param>
    /// <param name="roleGroupsJson">角色群組 JSON 字串。</param>
    /// <param name="functionPermissionsJson">功能權限 JSON 字串。</param>
    /// <returns>簽發完成的 JWT 字串。</returns>
    public async Task<string> GeneratePersonalTokenAsync(
        string userId,
        string email,
        string mobilePhone,
        string deptId,
        string ip,
        string? roleGroupsJson = null,
        string? functionPermissionsJson = null)
    {
        var settings = await GetSettingsAsync();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, userId),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.MobilePhone, mobilePhone),
            new("dept_id", deptId),
            new("ip", ip),
            new("token_type", TokenTypePersonal)
        };

        if (!string.IsNullOrWhiteSpace(roleGroupsJson))
            claims.Add(new Claim("role_groups", roleGroupsJson));

        if (!string.IsNullOrWhiteSpace(functionPermissionsJson))
            claims.Add(new Claim("function_permissions", functionPermissionsJson));

        return CreateToken(settings, claims, TimeSpan.FromMinutes(settings.PersonalTokenExpire));
    }

    /// <summary>
    /// 產生系統對系統呼叫使用的 JWT。
    /// </summary>
    /// <param name="clientId">呼叫端識別碼。</param>
    /// <param name="ip">請求來源 IP。</param>
    /// <returns>簽發完成的 JWT 字串。</returns>
    public async Task<string> GenerateServerTokenAsync(string clientId, string ip)
    {
        var settings = await GetSettingsAsync();
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, clientId),
            new("client_id", clientId),
            new("ip", ip),
            new("token_type", TokenTypeServer)
        };

        return CreateToken(settings, claims, TimeSpan.FromSeconds(settings.ServerTokenExpire));
    }

    /// <summary>
    /// 驗證 JWT 並回傳對應的 ClaimsPrincipal。
    /// </summary>
    /// <param name="token">待驗證的 JWT 字串。</param>
    /// <param name="validateRevocation">是否檢查 token 是否已撤銷。</param>
    /// <returns>驗證成功回傳 ClaimsPrincipal，失敗回傳 null。</returns>
    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token, bool validateRevocation = true)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var settings = await GetSettingsAsync();
        var parameters = BuildValidationParameters(settings);
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            var tokenId = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (validateRevocation && !string.IsNullOrWhiteSpace(tokenId) && tokenRevocationService.IsRevoked(tokenId))
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 取得目前 JWT 設定：核心參數來自環境變數，過期時間來自資料庫。
    /// </summary>
    /// <returns>JWT 設定內容。</returns>
    /// <exception cref="InvalidOperationException">必要設定缺漏或設定值格式不合法。</exception>
    public async Task<JwtSettingDto> GetSettingsAsync()
    {
        var core = GetRequiredJwtCoreSettingsFromConfiguration(configuration);

        var settings = await db.Sys_BasicSettings
            .AsNoTracking()
            .Where(s => s.Type == JwtSettingType)
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var result = new JwtSettingDto
        {
            SecretKey = core.SecretKey,
            Issuer = core.Issuer,
            Audience = core.Audience,
            PersonalTokenExpire = GetPositiveInt(settings, PersonalTokenExpire),
            ServerTokenExpire = GetPositiveInt(settings, ServerTokenExpire)
        };

        return result;
    }

    /// <summary>
    /// 更新 JWT 過期時間設定並寫入資料庫。
    /// </summary>
    /// <param name="request">前端送入的 JWT 設定更新內容。</param>
    /// <param name="updatedBy">執行更新的使用者識別碼。</param>
    /// <exception cref="ArgumentException">輸入內容未通過驗證。</exception>
    public async Task UpdateSettingsAsync(JwtSettingUpdateRequest request, string updatedBy)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateExpirationSettings(request);

        await UpsertAsync(PersonalTokenExpire, request.PersonalTokenExpire.ToString(), "Personal token expiration minutes", updatedBy);
        await UpsertAsync(ServerTokenExpire, request.ServerTokenExpire.ToString(), "SSO token expiration seconds", updatedBy);
        await db.SaveChangesAsync();
    }

    private static string CreateToken(JwtSettingDto settings, IEnumerable<Claim> sourceClaims, TimeSpan lifetime)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.Add(lifetime);
        var claims = sourceClaims.ToList();
        claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
        claims.Add(new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static TokenValidationParameters BuildValidationParameters(JwtSettingDto settings)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = settings.Issuer,
            ValidAudience = settings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    }

    private async Task UpsertAsync(string key, string value, string label, string updatedBy)
    {
        var entity = await db.Sys_BasicSettings.FirstOrDefaultAsync(s => s.Type == JwtSettingType && s.Key == key);
        if (entity is null)
        {
            db.Sys_BasicSettings.Add(new Sys_BasicSetting
            {
                Type = JwtSettingType,
                Key = key,
                Value = value,
                Label = label,
                CreatedTime = DateTime.UtcNow,
                CreatedId = updatedBy
            });
            return;
        }

        entity.Value = value;
        entity.Label = label;
    }

    private static void ValidateExpirationSettings(JwtSettingUpdateRequest settings)
    {
        if (settings.PersonalTokenExpire <= 0)
            throw new ArgumentException("PersonalTokenExpire must be greater than zero.", nameof(settings.PersonalTokenExpire));

        if (settings.ServerTokenExpire <= 0)
            throw new ArgumentException("ServerTokenExpire must be greater than zero.", nameof(settings.ServerTokenExpire));
    }

    private static int GetPositiveInt(Dictionary<string, string> settings, string key)
    {
        if (!settings.TryGetValue(key, out var value) || !int.TryParse(value, out var result) || result <= 0)
            throw new InvalidOperationException($"JwtSetting.{key} must be a positive integer.");

        return result;
    }

    private static JwtSettingDto GetRequiredJwtCoreSettingsFromConfiguration(IConfiguration configuration)
    {
        var secret = configuration["JwtSettings:SecretKey"]?.Trim();
        var issuer = configuration["JwtSettings:Issuer"]?.Trim();
        var audience = configuration["JwtSettings:Audience"]?.Trim();

        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("Missing required JWT environment variable: JwtSettings__SecretKey.");

        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("Missing required JWT environment variable: JwtSettings__Issuer.");

        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("Missing required JWT environment variable: JwtSettings__Audience.");

        if (Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("JwtSettings__SecretKey must be at least 32 bytes.");

        return new JwtSettingDto
        {
            SecretKey = secret,
            Issuer = issuer,
            Audience = audience
        };
    }
}
