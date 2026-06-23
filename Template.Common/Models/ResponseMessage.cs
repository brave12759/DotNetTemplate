using Template.Common.Enums;
using Template.Common.Extensions;

namespace Template.Common.Models;

/// <summary>
/// 回覆訊息
/// </summary>
public class ResponseMessage<T>
{
    /// <summary>
    /// 狀態碼
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 回覆內容
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 傳遞資料
    /// </summary>
    public T? Details { get; set; }

    public static ResponseMessage<T> Success(T? content, string message = "成功") =>
        new() { Status = 200, Message = message, Details = content };

    public static ResponseMessage<T> Fail(int status, string message) =>
        new() { Status = status, Message = message, Details = default };

    /// <summary>
    /// 以 MessageEnum 建立回傳訊息，自動帶入 Status 與預設 Description 訊息
    /// </summary>
    public static ResponseMessage<T> From(MessageEnum code, T? content = default, string? message = null) =>
        new()
        {
            Status = (int)code,
            Message = message ?? code.GetDescription(),
            Details = content
        };
}
