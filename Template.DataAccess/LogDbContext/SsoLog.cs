using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.LogDbContext;

/// <summary>
/// SSO 串接日誌。
/// </summary>
[Table("SsoLog")]
[Index(nameof(EventTime), Name = "IX_SsoLog_EventTime")]
[Index(nameof(OperatorId), nameof(EventTime), Name = "IX_SsoLog_OperatorId_EventTime")]
[Index(nameof(ClientId), nameof(EventTime), Name = "IX_SsoLog_ClientId_EventTime")]
public partial class SsoLog
{
    /// <summary>
    /// SSO 日誌流水號。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 事件發生時間（UTC）。
    /// </summary>
    public DateTime EventTime { get; set; }

    /// <summary>
    /// 操作者；外部系統流程通常等同 ClientId。
    /// </summary>
    [StringLength(100)]
    public string OperatorId { get; set; } = string.Empty;

    /// <summary>
    /// SSO ClientId。
    /// </summary>
    [StringLength(100)]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// SSO 事件名稱，例如 Login、ValidateToken。
    /// </summary>
    [StringLength(50)]
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// 執行結果。
    /// </summary>
    [StringLength(20)]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// 呼叫來源 IP。
    /// </summary>
    [StringLength(50)]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// 日誌訊息。
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 額外補充資訊 JSON。
    /// </summary>
    public string MetadataJson { get; set; } = string.Empty;
}
