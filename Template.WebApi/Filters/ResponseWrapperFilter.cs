using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Template.Common.Models;

namespace Template.WebApi.Filters;

/// <summary>
/// 全域回傳包裝 Filter：將所有 Action 回傳值統一包成 ResponseMessage&lt;T&gt;。
/// 若 Action 或 Controller 標記 [SkipResponseWrap] 則跳過。
/// </summary>
public class ResponseWrapperFilter : IResultFilter, IOrderedFilter
{
    /// <summary>
    /// Filter 執行順序（越大越晚）。
    /// Result 包裝放在較後段，確保先取得最終狀態碼與結果內容。
    /// </summary>
    public int Order => int.MaxValue;

    public void OnResultExecuting(ResultExecutingContext context)
    {
        // 已標記跳過
        if (context.ActionDescriptor.EndpointMetadata
                .Any(m => m is SkipResponseWrapAttribute))
            return;

        switch (context.Result)
        {
            // 已是 ResponseMessage<T>，不再包裝
            case ObjectResult { Value: not null } obj
                when obj.Value.GetType().IsGenericType &&
                     obj.Value.GetType().GetGenericTypeDefinition() == typeof(ResponseMessage<>):
                return;

            // 正常物件回傳
            case ObjectResult obj:
            {
                var statusCode = obj.StatusCode ?? context.HttpContext.Response.StatusCode;
                var wrapped = WrapValue(statusCode, obj.Value);
                context.Result = new ObjectResult(wrapped) { StatusCode = statusCode };
                break;
            }

            // 空回傳 (204 No Content / void)
            case EmptyResult:
            {
                var wrapped = ResponseMessage<object>.Success(null);
                context.Result = new ObjectResult(wrapped) { StatusCode = 200 };
                break;
            }
        }
    }

    public void OnResultExecuted(ResultExecutedContext context) { }

    private static object WrapValue(int statusCode, object? value)
    {
        var message = statusCode is >= 200 and < 300 ? "成功" : "錯誤";
        var responseType = typeof(ResponseMessage<>).MakeGenericType(value?.GetType() ?? typeof(object));
        var factory = statusCode is >= 200 and < 300
            ? responseType.GetMethod(nameof(ResponseMessage<object>.Success))!
            : responseType.GetMethod(nameof(ResponseMessage<object>.Fail))!;

        return statusCode is >= 200 and < 300
            ? factory.Invoke(null, [value, message])!
            : factory.Invoke(null, [statusCode, value?.ToString() ?? message])!;
    }
}
