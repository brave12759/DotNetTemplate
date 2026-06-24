using System.Security.Claims;
using Template.Common.Models.Jwt;

namespace Template.Common.Services;

/// <summary>
/// JWT 服務介面，定義簽發、驗證與設定管理能力。
/// </summary>
public interface IJwtService
{
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
    Task<string> GeneratePersonalTokenAsync(
        string userId,
        string email,
        string mobilePhone,
        string deptId,
        string ip,
        string? roleGroupsJson = null,
        string? functionPermissionsJson = null);

    /// <summary>
    /// 產生系統對系統呼叫使用的 JWT。
    /// </summary>
    /// <param name="clientId">呼叫端識別碼。</param>
    /// <param name="ip">請求來源 IP。</param>
    /// <returns>簽發完成的 JWT 字串。</returns>
    Task<string> GenerateServerTokenAsync(string clientId, string ip);

    /// <summary>
    /// 驗證 JWT 並回傳對應身分資訊。
    /// </summary>
    /// <param name="token">待驗證的 JWT 字串。</param>
    /// <param name="validateRevocation">是否檢查 token 是否已撤銷。</param>
    /// <returns>驗證成功回傳 ClaimsPrincipal，失敗回傳 null。</returns>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token, bool validateRevocation = true);

    /// <summary>
    /// 驗證已過期 JWT 的簽章與發行資訊，並回傳對應身分資訊。
    /// </summary>
    /// <param name="token">待驗證的已過期 JWT 字串。</param>
    /// <returns>驗證成功回傳 ClaimsPrincipal，失敗回傳 null。</returns>
    Task<ClaimsPrincipal?> ValidateExpiredTokenAsync(string token);

    /// <summary>
    /// 取得目前 JWT 設定。
    /// </summary>
    /// <returns>JWT 設定內容。</returns>
    Task<JwtSettingDto> GetSettingsAsync();

    /// <summary>
    /// 更新 JWT 設定。
    /// </summary>
    /// <param name="request">JWT 設定更新內容。</param>
    /// <param name="updatedBy">執行更新的使用者識別碼。</param>
    Task UpdateSettingsAsync(JwtSettingUpdateRequest request, string updatedBy);
}
