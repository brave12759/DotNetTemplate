using Template.Common.Enums;

namespace Template.BusinessRule.LogService.Services;

/// <summary>
/// 依日誌種類挑選對應 writer，避免業務服務知道每張日誌表的實作細節。
/// </summary>
public class LogWriterFactory(IEnumerable<ILogWriter> writers) : ILogWriterFactory
{
    private readonly Dictionary<LogWriterTypeEnum, ILogWriter> _writers = writers.ToDictionary(w => w.LogType);

    /// <inheritdoc />
    public ILogWriter Create(LogWriterTypeEnum logType)
    {
        if (_writers.TryGetValue(logType, out var writer))
            return writer;

        throw new NotSupportedException($"尚未註冊 {logType} 日誌寫入器。");
    }
}
