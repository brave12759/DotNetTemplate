namespace Template.Common.Models.User;

/// <summary>
/// 重設密碼請求。
/// </summary>
public class UserResetPasswordRequest
{
    public int Id { get; set; }

    public string NewPassword { get; set; } = string.Empty;
}
