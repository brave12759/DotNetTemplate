using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.ProjectDbContext;

/// <summary>
/// 使用者密碼歷程表，記錄每次密碼建立、重設與變更。
/// </summary>
[Table("Sys_UserPasswordHistory")]
[Index(nameof(UserId), nameof(ChangedTime), Name = "IX_Sys_UserPasswordHistory_UserId_ChangedTime")]
public partial class Sys_UserPasswordHistory
{
    /// <summary>
    /// 密碼歷程流水號。
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 使用者帳號。
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 當次密碼雜湊值。
    /// </summary>
    [Required]
    [StringLength(500)]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// 密碼異動類型：1 建立、2 重設、3 變更。
    /// </summary>
    public int ChangeType { get; set; }

    /// <summary>
    /// 密碼異動時間。
    /// </summary>
    public DateTime ChangedTime { get; set; }

    /// <summary>
    /// 密碼異動人員。
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ChangedId { get; set; } = string.Empty;
}
