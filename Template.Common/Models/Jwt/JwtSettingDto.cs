namespace Template.Common.Models.Jwt;

/// <summary>
/// 存放在資料庫中的 JWT 執行期設定。
/// </summary>
public class JwtSettingDto
{
    /// <summary>
    /// HMAC 簽章金鑰；一般 API 查詢不可完整回傳此值。
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Token 發行者。
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token 接收對象。
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// 個人 Token 有效時間，單位分鐘。
    /// </summary>
    public int PersonalTokenExpire { get; set; }

    /// <summary>
    /// SSO Server Token 有效時間，單位秒。
    /// </summary>
    public int ServerTokenExpire { get; set; }
}
