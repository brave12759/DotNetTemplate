namespace Template.Common.Models;

/// <summary>
/// 登入操作結果。
/// </summary>
public class LoginResult
{
    /// <summary>
    /// 是否登入成功。
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 登入成功時的 JWT Token。
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// 登入失敗時的錯誤訊息。
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// 帳號因多次登入失敗已被停用（區分一般登入失敗）。
    /// </summary>
    public bool AccountDisabled { get; init; }

    /// <summary>
    /// 建立登入成功結果。
    /// </summary>
    public static LoginResult Ok(string token) =>
        new() { Success = true, Token = token };

    /// <summary>
    /// 建立登入失敗結果。
    /// </summary>
    public static LoginResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };

    /// <summary>
    /// 建立帳號被停用結果（登入失敗超限）。
    /// </summary>
    public static LoginResult AccountLockedOut(string message) =>
        new() { Success = false, ErrorMessage = message, AccountDisabled = true };
}
