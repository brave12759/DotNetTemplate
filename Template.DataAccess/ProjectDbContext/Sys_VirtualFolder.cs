#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.ProjectDbContext;

/// <summary>
/// 檔案虛擬資料夾資料表
/// </summary>
[Table("Sys_VirtualFolder")]
[Index("Scope", "OwnerUserId", "FolderPath", Name = "UQ_Sys_VirtualFolder_Scope_Owner_Path", IsUnique = true)]
[Index("ParentFolderId", "SortOrder", Name = "IX_Sys_VirtualFolder_ParentFolderId_SortOrder")]
public partial class Sys_VirtualFolder
{
    /// <summary>
    /// 主鍵
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 範圍：1 Personal、2 Admin
    /// </summary>
    public int Scope { get; set; }

    /// <summary>
    /// 擁有者使用者帳號
    /// </summary>
    [Required]
    [StringLength(50)]
    public string OwnerUserId { get; set; }

    /// <summary>
    /// 資料夾名稱
    /// </summary>
    [Required]
    [StringLength(200)]
    public string FolderName { get; set; }

    /// <summary>
    /// 虛擬資料夾完整路徑
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FolderPath { get; set; }

    /// <summary>
    /// 父層資料夾 ID
    /// </summary>
    public long? ParentFolderId { get; set; }

    /// <summary>
    /// 同層排序值
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 啟用狀態
    /// </summary>
    public bool IsEnable { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 建立人員
    /// </summary>
    [Required]
    [StringLength(50)]
    public string CreatedId { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime UpdatedTime { get; set; }

    /// <summary>
    /// 更新人員
    /// </summary>
    [Required]
    [StringLength(50)]
    public string UpdatedId { get; set; }

    public virtual ICollection<Sys_VirtualFolder> InverseParentFolder { get; set; } = new List<Sys_VirtualFolder>();

    public virtual Sys_VirtualFolder ParentFolder { get; set; }
}
