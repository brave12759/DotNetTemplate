namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作佇列查詢服務。
/// </summary>
public interface IBackgroundJobMonitorService
{
    /// <summary>
    /// 取得背景工作佇列統計資料。
    /// </summary>
    Task<BackgroundJobSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢背景工作明細清單。
    /// </summary>
    Task<BackgroundJobQueryResult> GetListAsync(
        BackgroundWorkType? workType = null,
        BackgroundJobStatus? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得單一背景工作明細。
    /// </summary>
    Task<BackgroundJobDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
