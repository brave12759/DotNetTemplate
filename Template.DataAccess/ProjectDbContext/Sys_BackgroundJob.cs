using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.ProjectDbContext;

/// <summary>
/// 背景工作佇列表。
/// </summary>
[Table("Sys_BackgroundJob")]
[Index(nameof(WorkType), nameof(Status), nameof(ScheduledTime), nameof(Priority), Name = "IX_Sys_BackgroundJob_Dequeue")]
[Index(nameof(WorkKey), Name = "IX_Sys_BackgroundJob_WorkKey")]
public partial class Sys_BackgroundJob
{
    [Key]
    public long Id { get; set; }

    public int WorkType { get; set; }

    [StringLength(200)]
    public string WorkKey { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public int Priority { get; set; }

    public int Status { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetryCount { get; set; }

    public DateTime ScheduledTime { get; set; }

    public DateTime? StartedTime { get; set; }

    public DateTime? CompletedTime { get; set; }

    public DateTime? LockedUntil { get; set; }

    [StringLength(100)]
    public string LockedBy { get; set; } = string.Empty;

    public string LastError { get; set; } = string.Empty;

    public DateTime CreatedTime { get; set; }

    [Required]
    [StringLength(50)]
    public string CreatedId { get; set; } = string.Empty;

    public DateTime UpdatedTime { get; set; }

    [Required]
    [StringLength(50)]
    public string UpdatedId { get; set; } = string.Empty;

    [ConcurrencyCheck]
    public Guid Version { get; set; }
}
