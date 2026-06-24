using Microsoft.AspNetCore.Mvc;
using Template.Common.BackgroundQueue;

namespace Template.WebApi.Controllers;

/// <summary>
/// 背景工作佇列查詢 API。
/// </summary>
public class BackgroundQueueController(
    ILogger<BackgroundQueueController> logger,
    IBackgroundJobMonitorService backgroundJobMonitorService) : AuthenticationController<BackgroundQueueController>(logger)
{
    private readonly IBackgroundJobMonitorService _backgroundJobMonitorService = backgroundJobMonitorService;

    /// <summary>
    /// 取得背景工作佇列統計資料。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var summary = await _backgroundJobMonitorService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// 查詢背景工作明細清單。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] BackgroundWorkType? workType,
        [FromQuery] BackgroundJobStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var jobs = await _backgroundJobMonitorService.GetListAsync(
                workType,
                status,
                page,
                pageSize,
                cancellationToken);

            return Ok(jobs);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 取得單一背景工作明細。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken cancellationToken)
    {
        try
        {
            var job = await _backgroundJobMonitorService.GetByIdAsync(id, cancellationToken);
            if (job is null)
                return NotFound("查無背景工作資料。");

            return Ok(job);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
