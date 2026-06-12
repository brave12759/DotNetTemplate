namespace Template.BusinessRule.SsoService.Models;

/// <summary>
/// SSO Server Token 驗證結果；供其他系統確認 Token 是否由本系統發出且仍有效。
/// </summary>
public class SsoTokenValidateResult
{
    /// <summary>
    /// Token 是否有效。必須同時符合簽章正確、未過期、未撤銷、token_type 為 server，且 client 仍啟用。
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// Token 內記錄的 SSO client ID。
    /// </summary>
    public string ClientId { get; init; } = string.Empty;

    /// <summary>
    /// Token 到期時間。
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }
}
