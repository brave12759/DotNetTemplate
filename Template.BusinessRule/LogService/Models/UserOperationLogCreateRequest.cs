using Template.Common.Enums;

namespace Template.BusinessRule.LogService.Models;

/// <summary>
/// 建立使用者操作日誌請求。
/// </summary>
public class UserOperationLogCreateRequest
{
    /// <summary>
    /// 事件發生時間；未指定時由 LogService 使用目前 UTC 時間。
    /// </summary>
    public DateTime? EventTime { get; set; }

    /// <summary>
    /// 執行使用者代碼；未指定時由 LogService 從目前登入者帶入。
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 功能模組名稱，例如 User、Login。
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 操作種類。
    /// </summary>
    public AuditActionEnum Action { get; set; }

    /// <summary>
    /// 執行結果。
    /// </summary>
    public AuditResultEnum Result { get; set; } = AuditResultEnum.Success;

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
    /// 異動前資料；LogService 會序列化成 JSON。
    /// </summary>
    public object? OldValue { get; set; }

    /// <summary>
    /// 異動後資料；LogService 會序列化成 JSON。
    /// </summary>
    public object? NewValue { get; set; }

    /// <summary>
    /// 額外資訊；LogService 會序列化成 JSON。
    /// </summary>
    public object? Metadata { get; set; }
}
