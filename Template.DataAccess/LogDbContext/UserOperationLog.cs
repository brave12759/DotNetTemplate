using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.LogDbContext;

/// <summary>
/// 使用者操作日誌。
/// </summary>
[Table("UserOperationLog")]
[Index(nameof(EventTime), Name = "IX_UserOperationLog_EventTime")]
[Index(nameof(UserId), nameof(EventTime), Name = "IX_UserOperationLog_UserId_EventTime")]
[Index(nameof(Module), nameof(Action), nameof(EventTime), Name = "IX_UserOperationLog_Module_Action_EventTime")]
public partial class UserOperationLog
{
    /// <summary>
    /// 使用者操作日誌流水號。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 事件發生時間（UTC）。
    /// </summary>
    public DateTime EventTime { get; set; }

    /// <summary>
    /// 執行使用者代碼；登入失敗且帳號不存在時會記錄使用者輸入的帳號。
    /// </summary>
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 功能模組名稱，例如 User、Login。
    /// </summary>
    [StringLength(100)]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// 操作種類，對應 AuditActionEnum。
    /// </summary>
    public int Action { get; set; }

    /// <summary>
    /// 執行結果，對應 AuditResultEnum。
    /// </summary>
    public int Result { get; set; }

    /// <summary>
    /// 被操作資料類型。
    /// </summary>
    [StringLength(100)]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// 被操作資料主鍵或外部識別碼。
    /// </summary>
    [StringLength(200)]
    public string TargetId { get; set; } = string.Empty;

    /// <summary>
    /// 呼叫來源 IP。
    /// </summary>
    [StringLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 追蹤識別碼，用於串接應用程式 log。
    /// </summary>
    [StringLength(100)]
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// 操作訊息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 異動前資料 JSON；不可寫入密碼或 Token。
    /// </summary>
    public string OldValueJson { get; set; } = string.Empty;

    /// <summary>
    /// 異動後資料 JSON；不可寫入密碼或 Token。
    /// </summary>
    public string NewValueJson { get; set; } = string.Empty;

    /// <summary>
    /// 額外補充資訊 JSON。
    /// </summary>
    public string MetadataJson { get; set; } = string.Empty;
}
