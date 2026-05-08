using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Template.WebApi.Authentication;

/// <summary>
/// Development 環境專用認證 Handler。
/// <para>
/// 當請求未帶有 <c>Authorization: Bearer</c> 標頭時，自動以 <see cref="DevBypassUserSettings"/>
/// 設定的假用戶通過認證，使所有 <c>[Authorize]</c> 端點免登入即可使用。
/// </para>
/// <para>
/// 若請求攜帶 Bearer Token，則轉交 JWT Bearer Handler 驗證，確保開發者仍可測試真實 Token 流程。
/// </para>
/// </summary>
public class DevBypassAuthenticationHandler(
    IOptionsMonitor<DevBypassAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    DevBypassUserSettings devUser)
    : AuthenticationHandler<DevBypassAuthenticationOptions>(options, logger, encoder)
{
    /// <summary>DevBypass Scheme 名稱。</summary>
    public const string SchemeName = "DevBypass";

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 若有 Bearer Token → 轉交 JWT Bearer 驗證（開發者仍可測試真實 Token）
        if (Request.Headers.ContainsKey("Authorization"))
            return await Context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);

        // 無 Token → 自動以假用戶通過認證
        var now     = DateTimeOffset.UtcNow;
        var expires = now.AddHours(8);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name,                devUser.UserId),
            new Claim(ClaimTypes.Email,               devUser.Email),
            new Claim(ClaimTypes.MobilePhone,         devUser.MobilePhone),
            new Claim("dept_id",                      devUser.DeptId),
            new Claim("ip",                           "127.0.0.1"),
            new Claim(JwtRegisteredClaimNames.Jti,    Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,    now.ToUnixTimeSeconds().ToString(),     ClaimValueTypes.Integer64),
            new Claim(JwtRegisteredClaimNames.Exp,    expires.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        var identity  = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
