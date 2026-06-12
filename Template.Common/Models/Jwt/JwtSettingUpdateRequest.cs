namespace Template.Common.Models.Jwt;

/// <summary>
/// 更新 JWT 執行期設定的請求模型。
/// </summary>
public class JwtSettingUpdateRequest
{
    /// <summary>
    /// 個人 Token 有效時間，單位分鐘。
    /// </summary>
    public int PersonalTokenExpire { get; set; }

    /// <summary>
    /// SSO Server Token 有效時間，單位秒。
    /// </summary>
    public int ServerTokenExpire { get; set; }
}
