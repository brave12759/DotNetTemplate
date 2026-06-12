namespace Template.BusinessRule.LogService.Models;

/// <summary>
/// 使用者操作日誌查詢條件。
/// </summary>
public class UserOperationLogQueryRequest
{
    /// <summary>
    /// 起始時間（UTC）。
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 結束時間（UTC）。
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 執行使用者代碼。
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// 功能模組名稱。
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// 操作種類。
    /// </summary>
    public Template.Common.Enums.AuditActionEnum? Action { get; set; }

    /// <summary>
    /// 執行結果。
    /// </summary>
    public Template.Common.Enums.AuditResultEnum? Result { get; set; }

    /// <summary>
    /// 被操作資料類型。
    /// </summary>
    public string? TargetType { get; set; }

    /// <summary>
    /// 被操作資料主鍵或外部識別碼。
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// 頁碼，從 1 開始。
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每頁筆數，最大 200。
    /// </summary>
    public int PageSize { get; set; } = 50;
}
