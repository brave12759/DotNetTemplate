using Template.BusinessRule.SsoService.Enums;
using Template.Common.Extensions;

namespace Template.BusinessRule.SsoService.Models;

/// <summary>
/// SSO client 登入結果，登入成功時會帶回系統發出的 Server Token。
/// </summary>
public class SsoTokenResult
{
    /// <summary>
    /// 是否登入成功。
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 登入成功後取得的 Server Token。
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// 登入失敗時提供前端或串接系統判斷原因的固定代碼。
    /// </summary>
    public SsoMessageEnum? MessageCode { get; init; }

    /// <summary>
    /// 登入失敗時可直接顯示或紀錄的中文訊息。
    /// </summary>
    public string ErrorMessage { get; init; } = string.Empty;

    /// <summary>
    /// 建立登入成功結果。
    /// </summary>
    public static SsoTokenResult Ok(string token) => new() { Success = true, Token = token };

    /// <summary>
    /// 建立登入失敗結果，並同時保留固定代碼與中文描述。
    /// </summary>
    public static SsoTokenResult Fail(SsoMessageEnum messageCode) => new()
    {
        Success = false,
        MessageCode = messageCode,
        ErrorMessage = messageCode.GetDescription()
    };
}
