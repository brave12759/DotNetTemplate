using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Template.Common.Models;
using Template.Common.Services;

namespace Template.WebApi.Services;

/// <summary>
/// 從 HTTP 請求的 JWT Claims 解析當前登入使用者資訊。
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly Lazy<CurrentUser> _currentUser;

    /// <summary>
    /// 建立當前使用者服務。
    /// </summary>
    /// <param name="httpContextAccessor">HTTP 內容存取器。</param>
    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _currentUser = new Lazy<CurrentUser>(() => Parse(httpContextAccessor.HttpContext));
    }

    /// <inheritdoc />
    public CurrentUser CurrentUser => _currentUser.Value;

    private static CurrentUser Parse(HttpContext? context)
    {
        if (context?.User?.Identity?.IsAuthenticated != true)
            return new CurrentUser();

        var claims = context.User.Claims;

        return new CurrentUser
        {
            UserId      = GetClaim(claims, ClaimTypes.Name),
            Email       = GetClaim(claims, ClaimTypes.Email),
            MobilePhone = GetClaim(claims, ClaimTypes.MobilePhone),
            DeptId      = GetClaim(claims, "dept_id"),
            Ip          = GetClaim(claims, "ip"),
            IssuedTime  = long.TryParse(GetClaim(claims, JwtRegisteredClaimNames.Iat), out var iat) ? iat : 0,
            ExpiredTime = long.TryParse(GetClaim(claims, JwtRegisteredClaimNames.Exp), out var exp) ? exp : 0,
            TokenId     = GetClaim(claims, JwtRegisteredClaimNames.Jti),
        };
    }

    private static string GetClaim(IEnumerable<Claim> claims, string type)
        => claims.FirstOrDefault(c => c.Type == type)?.Value ?? string.Empty;
}
