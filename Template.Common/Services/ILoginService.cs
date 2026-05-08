using Template.Common.Models;

namespace Template.Common.Services;

/// <summary>
/// 登入服務介面。
/// </summary>
public interface ILoginService
{
    /// <summary>
    /// 驗證帳號密碼並發放 JWT Token。
    /// </summary>
    /// <param name="userId">使用者帳號。</param>
    /// <param name="password">明文密碼。</param>
    /// <param name="ip">登入來源 IP。</param>
    /// <returns>登入結果。</returns>
    Task<LoginResult> LoginAsync(string userId, string password, string ip);

    /// <summary>
    /// 【開發專用】不驗證密碼，直接以指定帳號資料發放 JWT Token。
    /// </summary>
    /// <param name="userId">使用者帳號。</param>
    /// <param name="ip">登入來源 IP。</param>
    /// <returns>登入結果。</returns>
    Task<LoginResult> DevLoginAsync(string userId, string ip);

    /// <summary>
    /// 撤銷目前 JWT Token（登出）。
    /// </summary>
    /// <param name="tokenId">JWT jti。</param>
    /// <param name="expiredUnixTimeSeconds">JWT exp（Unix Timestamp）。</param>
    Task LogoutAsync(string tokenId, long expiredUnixTimeSeconds);
}
