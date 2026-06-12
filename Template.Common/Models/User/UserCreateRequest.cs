namespace Template.Common.Models.User;

/// <summary>
/// 建立使用者請求。
/// </summary>
public class UserCreateRequest
{
    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 密碼（空白時自動套用系統預設密碼）。
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 使用者所屬部門 ID。
    /// </summary>
    public int DeptId { get; set; }

    public string MobilePhone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsEnable { get; set; } = true;
}
