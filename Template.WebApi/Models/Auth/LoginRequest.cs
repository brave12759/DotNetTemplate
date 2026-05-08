using System.ComponentModel.DataAnnotations;

namespace Template.WebApi.Models.Auth;

/// <summary>
/// 登入請求。
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 使用者帳號。
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 密碼。
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
