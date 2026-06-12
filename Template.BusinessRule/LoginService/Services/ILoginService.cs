using Template.Common.Models;

namespace Template.BusinessRule.LoginService.Services;

/// <summary>
/// 登入服務介面。
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// 驗證使用者帳號密碼並簽發 JWT Token。
    /// </summary>
    Task<LoginResult> LoginAsync(string userId, string password, string ip);

    /// <summary>
    /// 開發環境用假登入，不驗證密碼並簽發 JWT Token。
    /// </summary>
    Task<LoginResult> DevLoginAsync(string userId, string ip);

    /// <summary>
    /// 重新簽發使用者 JWT Token，並撤銷舊 Token。
    /// </summary>
    Task<LoginResult> RefreshAsync(string userId, string tokenId, long expiredUnixTimeSeconds, string ip);

    /// <summary>
    /// 登出時撤銷目前 JWT Token。
    /// </summary>
    Task LogoutAsync(string tokenId, long expiredUnixTimeSeconds);
}
