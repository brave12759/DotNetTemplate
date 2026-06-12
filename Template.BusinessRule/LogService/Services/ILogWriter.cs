using Template.Common.Enums;

namespace Template.BusinessRule.LogService.Services;

/// <summary>
/// 日誌寫入器介面；不同日誌資料表可各自實作一個 writer。
/// </summary>
public interface ILogWriter
{
    /// <summary>
    /// 此 writer 負責的日誌種類。
    /// </summary>
    LogWriterTypeEnum LogType { get; }

    /// <summary>
    /// 寫入日誌。
    /// </summary>
    Task<long> WriteAsync(object request, CancellationToken cancellationToken = default);
}
