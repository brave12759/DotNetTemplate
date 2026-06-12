namespace Template.BusinessRule.SsoService.Models;

/// <summary>
/// SSO client 輸出模型；只回傳識別與稽核資料，不回傳 secret 或 secret hash。
/// </summary>
public class SsoClientDto
{
    /// <summary>
    /// 流水號主鍵。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 外部系統的 client ID，外部系統登入 SSO 時會使用此值。
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// 外部系統顯示名稱，供後台管理辨識用途。
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// 是否啟用；停用後該 client 不能登入，既有 Server Token 驗證也會失敗。
    /// </summary>
    public bool IsEnable { get; set; }

    /// <summary>
    /// 建立時間。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 建立者帳號。
    /// </summary>
    public string CreatedId { get; set; } = string.Empty;

    /// <summary>
    /// 最後更新時間。
    /// </summary>
    public DateTime UpdatedTime { get; set; }

    /// <summary>
    /// 最後更新者帳號。
    /// </summary>
    public string UpdatedId { get; set; } = string.Empty;
}
