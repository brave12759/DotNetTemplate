namespace Template.BusinessRule.LogService.Models;

/// <summary>
/// 使用者操作日誌輸出資料。
/// </summary>
public class UserOperationLogDto
{
    /// <summary>
    /// 使用者操作日誌流水號。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 事件發生時間（UTC）。
    /// </summary>
    public DateTime EventTime { get; set; }

    /// <summary>
    /// 執行使用者代碼。
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 功能模組名稱。
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 操作種類。
    /// </summary>
    public Template.Common.Enums.AuditActionEnum Action { get; set; }

    /// <summary>
    /// 操作種類名稱。
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// 執行結果。
    /// </summary>
    public Template.Common.Enums.AuditResultEnum Result { get; set; }

    /// <summary>
    /// 執行結果名稱。
    /// </summary>
    public string ResultName { get; set; } = string.Empty;

    /// <summary>
    /// 被操作資料類型。
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// 被操作資料主鍵或外部識別碼。
    /// </summary>
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// 呼叫來源 IP。
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 追蹤識別碼。
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 操作訊息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 異動前資料 JSON。
    /// </summary>
    public string OldValueJson { get; set; } = string.Empty;

    /// <summary>
    /// 異動後資料 JSON。
    /// </summary>
    public string NewValueJson { get; set; } = string.Empty;

    /// <summary>
    /// 額外補充資訊 JSON。
    /// </summary>
    public string MetadataJson { get; set; } = string.Empty;
}
