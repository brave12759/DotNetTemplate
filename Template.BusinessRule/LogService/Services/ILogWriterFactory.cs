using Template.Common.Enums;

namespace Template.BusinessRule.LogService.Services;

/// <summary>
/// 日誌寫入器工廠。
/// </summary>
public interface ILogWriterFactory
{
    /// <summary>
    /// 依日誌種類取得對應 writer。
    /// </summary>
    ILogWriter Create(LogWriterTypeEnum logType);
}
