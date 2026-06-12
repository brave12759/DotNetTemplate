namespace Template.Common.Models.User;

/// <summary>
/// 使用者資料傳輸物件（不含密碼）。
/// </summary>
public class UserDto
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 使用者所屬部門 ID。
    /// </summary>
    public int DeptId { get; set; }

    /// <summary>
    /// 使用者所屬部門名稱。
    /// </summary>
    public string DeptName { get; set; } = string.Empty;

    public string MobilePhone { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public int LoginFailCount { get; set; }

    public bool IsEnable { get; set; }

    public DateTime? LastLoginTime { get; set; }

    public string LastLoginIp { get; set; } = string.Empty;

    public DateTime CreatedTime { get; set; }

    public DateTime? UpdatedTime { get; set; }
}
