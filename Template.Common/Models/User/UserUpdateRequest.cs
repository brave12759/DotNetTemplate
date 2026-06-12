namespace Template.Common.Models.User;

/// <summary>
/// 更新使用者請求（不含密碼）。
/// </summary>
public class UserUpdateRequest
{
    public int Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 使用者所屬部門 ID。
    /// </summary>
    public int DeptId { get; set; }

    public string MobilePhone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsEnable { get; set; }
}
