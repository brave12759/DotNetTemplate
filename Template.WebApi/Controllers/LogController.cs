using Microsoft.AspNetCore.Mvc;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.Common.Enums;
using Template.WebApi.Filters;

namespace Template.WebApi.Controllers;

/// <summary>
/// 日誌查詢 API。
/// </summary>
public class LogController(
    ILogger<LogController> logger,
    ILogService logService) : AuthenticationController<LogController>(logger)
{
    private const string ViewUserOperationLogPermission = "System.UserOperationLog:View";
    private const string ViewQueueLogPermission = "System.QueueLog:View";
    private const string ViewSsoLogPermission = "System.SsoLog:View";

    /// <summary>
    /// 查詢使用者操作日誌。
    /// </summary>
    [HttpGet]
    [RequirePermission(ViewUserOperationLogPermission)]
    public async Task<IActionResult> UserOperationLogs(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? userId,
        [FromQuery] string? module,
        [FromQuery] AuditActionEnum? action,
        [FromQuery] AuditResultEnum? result,
        [FromQuery] string? targetType,
        [FromQuery] string? targetId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var logs = await logService.GetUserOperationLogsAsync(new UserOperationLogQueryRequest
            {
                StartTime = startTime,
                EndTime = endTime,
                UserId = userId,
                Module = module,
                Action = action,
                Result = result,
                TargetType = targetType,
                TargetId = targetId,
                Page = page,
                PageSize = pageSize
            });

            return Ok(logs);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 查詢佇列日誌。
    /// </summary>
    [HttpGet]
    [RequirePermission(ViewQueueLogPermission)]
    public async Task<IActionResult> QueueLogs(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? operatorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var logs = await logService.GetQueueLogsAsync(new QueueLogQueryRequest
            {
                StartTime = startTime,
                EndTime = endTime,
                OperatorId = operatorId,
                Page = page,
                PageSize = pageSize
            });

            return Ok(logs);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 查詢 SSO 日誌。
    /// </summary>
    [HttpGet]
    [RequirePermission(ViewSsoLogPermission)]
    public async Task<IActionResult> SsoLogs(
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime,
        [FromQuery] string? operatorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var logs = await logService.GetSsoLogsAsync(new SsoLogQueryRequest
            {
                StartTime = startTime,
                EndTime = endTime,
                OperatorId = operatorId,
                Page = page,
                PageSize = pageSize
            });

            return Ok(logs);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
