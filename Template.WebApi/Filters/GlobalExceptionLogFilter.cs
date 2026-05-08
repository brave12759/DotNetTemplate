using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Template.Common.Models;

namespace Template.WebApi.Filters;

/// <summary>
/// 全域例外紀錄 Filter。
/// 捕捉未處理例外並寫入 Serilog，避免錯誤資訊遺漏。
/// </summary>
public class GlobalExceptionLogFilter(ILogger<GlobalExceptionLogFilter> logger) : IExceptionFilter, IOrderedFilter
{
    private const string GenericErrorMessage = "系統發生未預期錯誤，請稍後再試。";

    /// <summary>
    /// Filter 執行順序（越小越早）。
    /// 例外處理需最先攔截，避免被其他 Filter 干擾。
    /// </summary>
    public int Order => int.MinValue;

    /// <inheritdoc />
    public void OnException(ExceptionContext context)
    {
        var httpContext = context.HttpContext;
        var request = httpContext.Request;

        var userId = httpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        var tokenId = httpContext.User.FindFirst("jti")?.Value ?? string.Empty;

        logger.LogError(
            context.Exception,
            "Unhandled exception. TraceId={TraceId}, Method={Method}, Path={Path}, QueryString={QueryString}, UserId={UserId}, TokenId={TokenId}, Ip={Ip}",
            httpContext.TraceIdentifier,
            request.Method,
            request.Path.Value,
            request.QueryString.Value,
            userId,
            tokenId,
            httpContext.Connection.RemoteIpAddress?.ToString());

        context.Result = new ObjectResult(ResponseMessage<object>.Fail(500, GenericErrorMessage))
        {
            StatusCode = 500
        };

        context.ExceptionHandled = true;
    }
}
