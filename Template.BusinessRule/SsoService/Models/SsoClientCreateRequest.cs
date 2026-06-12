namespace Template.BusinessRule.SsoService.Models;

/// <summary>
/// 建立 SSO client 的請求模型。
/// </summary>
public class SsoClientCreateRequest
{
    /// <summary>
    /// 外部系統的 client ID，需提供給外部系統作為登入帳號。
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 外部系統顯示名稱，供後台管理辨識用途。
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// 明文 client secret。只在建立時接收，儲存前會雜湊，不會再透過 API 回傳。
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// 是否啟用此 SSO client。
    /// </summary>
    public bool IsEnable { get; set; } = true;
}
