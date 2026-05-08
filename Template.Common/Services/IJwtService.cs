namespace Template.Common.Services;

/// <summary>
/// JWT Token 產生服務介面。
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// 產生包含使用者資訊的 JWT Token。
    /// </summary>
    /// <param name="userId">使用者帳號。</param>
    /// <param name="email">電子郵件。</param>
    /// <param name="mobilePhone">聯絡電話。</param>
    /// <param name="deptId">部門 ID。</param>
    /// <param name="ip">登入來源 IP。</param>
    /// <returns>簽發的 JWT Token 字串。</returns>
    string GenerateToken(string userId, string email, string mobilePhone, string deptId, string ip);
}
