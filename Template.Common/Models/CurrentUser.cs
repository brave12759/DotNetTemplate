namespace Template.Common.Models;

/// <summary>
/// 當前登入使用者資訊（由 JWT Claims 解析）。
/// </summary>
public class CurrentUser
{
    /// <summary>
    /// 使用者帳號。
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 電子郵件。
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 聯絡電話。
    /// </summary>
    public string MobilePhone { get; set; } = string.Empty;

    /// <summary>
    /// 部門 ID。
    /// </summary>
    public string DeptId { get; set; } = string.Empty;

    /// <summary>
    /// 登入來源 IP。
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// JWT Token 簽發時間（Unix Timestamp）。
    /// </summary>
    public long IssuedTime { get; set; }

    /// <summary>
    /// JWT Token 到期時間（Unix Timestamp）。
    /// </summary>
    public long ExpiredTime { get; set; }

    /// <summary>
    /// JWT Token 唯一識別碼（jti）。
    /// </summary>
    public string TokenId { get; set; } = string.Empty;
}
