using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.LogDbContext;

/// <summary>
/// 佇列執行日誌。
/// </summary>
[Table("QueueLog")]
[Index(nameof(EventTime), Name = "IX_QueueLog_EventTime")]
[Index(nameof(OperatorId), nameof(EventTime), Name = "IX_QueueLog_OperatorId_EventTime")]
[Index(nameof(JobId), nameof(EventTime), Name = "IX_QueueLog_JobId_EventTime")]
public partial class QueueLog
{
    /// <summary>
    /// 佇列日誌流水號。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 事件發生時間（UTC）。
    /// </summary>
    public DateTime EventTime { get; set; }

    /// <summary>
    /// 操作者或背景工作執行者。
    /// </summary>
    [StringLength(50)]
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// 背景工作 ID。
    /// </summary>
    public long JobId { get; set; }

    /// <summary>
    /// 工作種類。
    /// </summary>
    public int WorkType { get; set; }

    /// <summary>
    /// 工作識別鍵。
    /// </summary>
    [StringLength(200)]
    public string WorkKey { get; set; } = string.Empty;

    /// <summary>
    /// 佇列事件名稱，例如 Enqueue、Claim、Complete、Fail。
    /// </summary>
    [StringLength(50)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// 工作狀態。
    /// </summary>
    public int Status { get; set; }

    /// <summary>
    /// 目前重試次數。
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 日誌訊息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 錯誤訊息。
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// 額外補充資訊 JSON。
    /// </summary>
    public string MetadataJson { get; set; } = string.Empty;
}
