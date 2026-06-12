using Template.BusinessRule.SsoService.Enums;
using Template.Common.Extensions;

namespace Template.BusinessRule.SsoService.Exceptions;

/// <summary>
/// SSO 服務層驗證失敗時使用的例外，讓 API 可以依照 MessageCode 回傳固定 enum 代碼。
/// </summary>
public class SsoMessageException : ArgumentException
{
    /// <summary>
    /// 可提供前端判斷錯誤類型的固定訊息代碼。
    /// </summary>
    public SsoMessageEnum MessageCode { get; }

    /// <summary>
    /// 建立 SSO 訊息例外。
    /// </summary>
    public SsoMessageException(SsoMessageEnum messageCode, string? paramName = null)
        : base(messageCode.GetDescription(), paramName)
    {
        MessageCode = messageCode;
    }
}
