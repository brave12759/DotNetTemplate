namespace Template.Common.Models.User;

/// <summary>
/// 修改密碼請求。
/// </summary>
public class UserChangePasswordRequest
{
    /// <summary>
    /// 使用者主鍵。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 舊密碼（需與現有密碼一致）。
    /// </summary>
    public string OldPassword { get; set; } = string.Empty;

    /// <summary>
    /// 新密碼（需符合密碼規則）。
    /// </summary>
    public string NewPassword { get; set; } = string.Empty;
}
