#nullable disable
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Template.DataAccess.ProjectDbContext;

/// <summary>
/// 檔案主檔資料表
/// </summary>
[Table("Sys_Attachment")]
[Index("FileId", Name = "UQ_Sys_Attachment_FileId", IsUnique = true)]
[Index("StorageKey", Name = "UQ_Sys_Attachment_StorageKey", IsUnique = true)]
[Index("Scope", "OwnerUserId", "CreatedTime", Name = "IX_Sys_Attachment_Scope_Owner_CreatedTime")]
[Index("VirtualFolderId", "CreatedTime", Name = "IX_Sys_Attachment_VirtualFolderId_CreatedTime")]
public partial class Sys_Attachment
{
    /// <summary>
    /// 主鍵
    /// </summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// 對外檔案識別碼
    /// </summary>
    public Guid FileId { get; set; }

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
    /// 原始檔名
    /// </summary>
    [Required]
    [StringLength(255)]
    public string FileName { get; set; }

    /// <summary>
    /// 副檔名（不含 .）
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Extension { get; set; }

    /// <summary>
    /// MIME 類型
    /// </summary>
    [Required]
    [StringLength(200)]
    public string ContentType { get; set; }

    /// <summary>
    /// 檔案大小（Bytes）
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// 虛擬資料夾 ID
    /// </summary>
    public long? VirtualFolderId { get; set; }

    /// <summary>
    /// 虛擬資料夾路徑快照
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FolderPath { get; set; }

    /// <summary>
    /// 儲存供應商名稱
    /// </summary>
    [Required]
    [StringLength(50)]
    public string StorageProvider { get; set; }

    /// <summary>
    /// 儲存鍵值（供應商端路徑/Key）
    /// </summary>
    [Required]
    [StringLength(500)]
    public string StorageKey { get; set; }

    /// <summary>
    /// 上傳模式：1 Single、2 Chunk
    /// </summary>
    public int UploadMode { get; set; }

    /// <summary>
    /// 上傳狀態：1 Pending、2 Uploading、3 Ready、4 Failed、5 Deleted
    /// </summary>
    public int UploadStatus { get; set; }

    /// <summary>
    /// 是否為 chunk 上傳
    /// </summary>
    public bool IsChunked { get; set; }

    /// <summary>
    /// chunk 數量
    /// </summary>
    public int ChunkCount { get; set; }

    /// <summary>
    /// 額外資訊 JSON
    /// </summary>
    [Required]
    public string MetadataJson { get; set; }

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

    public virtual Sys_VirtualFolder VirtualFolder { get; set; }
}
