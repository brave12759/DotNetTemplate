namespace Template.BusinessRule.SsoService.Models;

/// <summary>
/// 更新 SSO client 的請求模型。
/// </summary>
public class SsoClientUpdateRequest
{
    /// <summary>
    /// SSO client 流水號主鍵。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 外部系統顯示名稱。
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// 新的明文 client secret；空值代表保留原本的 secret。
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// 是否啟用此 SSO client。
    /// </summary>
    public bool IsEnable { get; set; } = true;
}
