namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作處理器。
/// </summary>
public interface IBackgroundJobHandler
{
    /// <summary>
    /// 此處理器負責的工作類型。
    /// </summary>
    BackgroundWorkType WorkType { get; }

    /// <summary>
    /// 執行背景工作。
    /// </summary>
    Task HandleAsync(BackgroundJob job, CancellationToken cancellationToken);
}
