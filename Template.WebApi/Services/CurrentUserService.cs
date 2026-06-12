using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Template.Common.Models;
using Template.Common.Services;

namespace Template.WebApi.Services;

/// <summary>
/// 將目前 HTTP 使用者的 claims 解析成 CurrentUser 模型。
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly Lazy<CurrentUser> _currentUser;

    /// <summary>
    /// 建立目前 HTTP context 對應的使用者解析服務。
    /// </summary>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _currentUser = new Lazy<CurrentUser>(() => Parse(httpContextAccessor.HttpContext));
    }

    /// <inheritdoc />
    public CurrentUser CurrentUser => _currentUser.Value;

    /// <summary>
    /// 從已驗證的 claims 建立 CurrentUser 模型。
    /// </summary>
    private static CurrentUser Parse(HttpContext? context)
    {
        if (context?.User?.Identity?.IsAuthenticated != true)
            return new CurrentUser();

        var claims = context.User.Claims;

        return new CurrentUser
        {
            UserId = GetClaim(claims, ClaimTypes.Name),
            Email = GetClaim(claims, ClaimTypes.Email),
            MobilePhone = GetClaim(claims, ClaimTypes.MobilePhone),
            DeptId = GetClaim(claims, "dept_id"),
            Ip = GetClaim(claims, "ip"),
            IssuedTime = long.TryParse(GetClaim(claims, JwtRegisteredClaimNames.Iat), out var iat) ? iat : 0,
            ExpiredTime = long.TryParse(GetClaim(claims, JwtRegisteredClaimNames.Exp), out var exp) ? exp : 0,
            TokenId = GetClaim(claims, JwtRegisteredClaimNames.Jti)
        };
    }

    /// <summary>
    /// 依 claim type 取得第一筆 claim 值；找不到時回傳空字串。
    /// </summary>
    private static string GetClaim(IEnumerable<Claim> claims, string type)
        => claims.FirstOrDefault(c => c.Type == type)?.Value ?? string.Empty;
}
