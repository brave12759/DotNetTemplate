using Template.BusinessRule.LogService.Models;

namespace Template.BusinessRule.LogService.Services;

/// <summary>
/// 日誌服務入口。
/// </summary>
public interface ILogService
{
    /// <summary>
    /// 寫入使用者操作日誌。
    /// </summary>
    Task<long> WriteUserOperationAsync(UserOperationLogCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢使用者操作日誌。
    /// </summary>
    Task<UserOperationLogQueryResult> GetUserOperationLogsAsync(UserOperationLogQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 寫入佇列日誌。
    /// </summary>
    Task<long> WriteQueueAsync(QueueLogCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢佇列日誌。
    /// </summary>
    Task<QueueLogQueryResult> GetQueueLogsAsync(QueueLogQueryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 寫入 SSO 日誌。
    /// </summary>
    Task<long> WriteSsoAsync(SsoLogCreateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查詢 SSO 日誌。
    /// </summary>
    Task<SsoLogQueryResult> GetSsoLogsAsync(SsoLogQueryRequest request, CancellationToken cancellationToken = default);
}
