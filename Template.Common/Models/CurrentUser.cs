namespace Template.Common.Models;

/// <summary>
/// 從 JWT claims 解析出的目前登入使用者資訊。
/// </summary>
public class CurrentUser
{
    /// <summary>
    /// 使用者帳號。
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 使用者 Email。
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 使用者手機。
    /// </summary>
    public string MobilePhone { get; set; } = string.Empty;

    /// <summary>
    /// Token 內的部門 ID。
    /// </summary>
    public string DeptId { get; set; } = string.Empty;

    /// <summary>
    /// Token 內記錄的登入或請求 IP。
    /// </summary>
    public string Ip { get; set; } = string.Empty;

    /// <summary>
    /// Token 發行時間，Unix timestamp。
    /// </summary>
    public long IssuedTime { get; set; }

    /// <summary>
    /// Token 到期時間，Unix timestamp。
    /// </summary>
    public long ExpiredTime { get; set; }

    /// <summary>
    /// jti claim 內的 Token ID。
    /// </summary>
    public string TokenId { get; set; } = string.Empty;
}
