using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.ProjectDbContext;

/// <summary>
/// 系統選單樹資料表。
/// </summary>
[Table("Sys_MenuTree")]
[Index(nameof(MenuCode), Name = "UQ_Sys_MenuTree_MenuCode", IsUnique = true)]
[Index(nameof(ParentId), nameof(SortOrder), Name = "IX_Sys_MenuTree_ParentId_SortOrder")]
[Index(nameof(IsEnable), Name = "IX_Sys_MenuTree_IsEnable")]
public partial class Sys_MenuTree
{
    /// <summary>
    /// 主鍵。
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// 父層選單 ID，Null 表示根選單。
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// 唯一選單代碼。
    /// </summary>
    [Required]
    [StringLength(100)]
    public string MenuCode { get; set; } = string.Empty;

    /// <summary>
    /// 顯示選單名稱。
    /// </summary>
    [Required]
    [StringLength(100)]
    public string MenuName { get; set; } = string.Empty;

    /// <summary>
    /// 圖示名稱。
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// 同層排序。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 啟用狀態。
    /// </summary>
    public bool IsEnable { get; set; }

    /// <summary>
    /// 建立時間。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 建立人員。
    /// </summary>
    [Required]
    [StringLength(50)]
    public string CreatedId { get; set; } = string.Empty;

    /// <summary>
    /// 更新時間。
    /// </summary>
    public DateTime UpdatedTime { get; set; }

    /// <summary>
    /// 更新人員。
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UpdatedId { get; set; } = string.Empty;
}
