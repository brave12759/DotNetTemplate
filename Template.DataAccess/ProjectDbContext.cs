using Microsoft.EntityFrameworkCore;
using Template.DataAccess.Extensions;

namespace Template.DataAccess.ProjectDbContext;

public partial class ProjectDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sys_MenuTree>(entity =>
        {
            entity.ToTable("Sys_MenuTree", tb => tb.HasComment("系統選單樹資料表"));

            entity.Property(e => e.Id).HasComment("主鍵");
            entity.Property(e => e.ParentId).HasComment("父層選單 ID");
            entity.Property(e => e.MenuCode).HasComment("唯一選單代碼");
            entity.Property(e => e.MenuName).HasComment("顯示選單名稱");
            entity.Property(e => e.Icon).HasComment("圖示名稱");
            entity.Property(e => e.SortOrder).HasComment("同層排序");
            entity.Property(e => e.IsEnable)
                .HasDefaultValue(true)
                .HasComment("啟用狀態");
            entity.Property(e => e.CreatedTime)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasComment("建立時間");
            entity.Property(e => e.CreatedId).HasComment("建立人員");
            entity.Property(e => e.UpdatedTime)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasComment("更新時間");
            entity.Property(e => e.UpdatedId).HasComment("更新人員");

            entity.HasOne(e => e.Parent)
                .WithMany(e => e.InverseParent)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Sys_MenuTree_Parent");
        });

        modelBuilder.Entity<Sys_BackgroundJob>(entity =>
        {
            entity.ToTable("Sys_BackgroundJob", tb => tb.HasComment("背景工作佇列表"));

            entity.Property(e => e.Id).HasComment("主鍵");
            entity.Property(e => e.WorkType).HasComment("工作類型");
            entity.Property(e => e.WorkKey).HasComment("工作業務鍵值");
            entity.Property(e => e.PayloadJson).HasComment("工作參數 JSON");
            entity.Property(e => e.Priority).HasDefaultValue(0).HasComment("優先序，數字越小越優先");
            entity.Property(e => e.Status).HasDefaultValue(0).HasComment("工作狀態");
            entity.Property(e => e.RetryCount).HasDefaultValue(0).HasComment("已重試次數");
            entity.Property(e => e.MaxRetryCount).HasDefaultValue(3).HasComment("最大重試次數");
            entity.Property(e => e.ScheduledTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("預計執行時間");
            entity.Property(e => e.StartedTime).HasComment("開始執行時間");
            entity.Property(e => e.CompletedTime).HasComment("完成時間");
            entity.Property(e => e.LockedUntil).HasComment("鎖定到期時間");
            entity.Property(e => e.LockedBy).HasComment("鎖定 worker");
            entity.Property(e => e.LastError).HasComment("最後錯誤訊息");
            entity.Property(e => e.CreatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("建立時間");
            entity.Property(e => e.CreatedId).HasComment("建立人員");
            entity.Property(e => e.UpdatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("更新時間");
            entity.Property(e => e.UpdatedId).HasComment("更新人員");
            entity.Property(e => e.Version).HasDefaultValueSql("(newid())").HasComment("樂觀鎖版本");
        });

        modelBuilder.Entity<Sys_RoleGroup>(entity =>
        {
            entity.ToTable("Sys_RoleGroup", tb => tb.HasComment("系統角色群組資料表"));

            entity.Property(e => e.RoleGroupId).HasComment("角色群組 ID");
            entity.Property(e => e.ParentRoleGroupId).HasComment("上層角色群組 ID");
            entity.Property(e => e.RoleGroupName).HasComment("角色群組名稱");
            entity.Property(e => e.Description).HasDefaultValue(string.Empty).HasComment("角色群組描述");
            entity.Property(e => e.SortOrder).HasDefaultValue(0).HasComment("排序值");
            entity.Property(e => e.IsEnable).HasDefaultValue(true).HasComment("啟用狀態");
            entity.Property(e => e.CreatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("建立時間");
            entity.Property(e => e.CreatedId).HasComment("建立人員");
            entity.Property(e => e.UpdatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("更新時間");
            entity.Property(e => e.UpdatedId).HasComment("更新人員");

            entity.HasOne(e => e.ParentRoleGroup)
                .WithMany(e => e.InverseParentRoleGroup)
                .HasForeignKey(e => e.ParentRoleGroupId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Sys_RoleGroup_Parent");
        });

        modelBuilder.Entity<Sys_UserRoleGroup>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleGroupId })
                .HasName("PK_Sys_UserRoleGroup");

            entity.ToTable("Sys_UserRoleGroup", tb => tb.HasComment("使用者角色群組對應表"));

            entity.Property(e => e.UserId).HasComment("使用者帳號");
            entity.Property(e => e.RoleGroupId).HasComment("角色群組 ID");
            entity.Property(e => e.CreatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("建立時間");
            entity.Property(e => e.CreatedId).HasComment("建立人員");

            entity.HasOne(e => e.RoleGroup)
                .WithMany(e => e.Sys_UserRoleGroups)
                .HasForeignKey(e => e.RoleGroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Sys_UserRoleGroup_RoleGroup");

            entity.HasOne(e => e.User)
                .WithMany(e => e.Sys_UserRoleGroups)
                .HasPrincipalKey(e => e.UserId)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Sys_UserRoleGroup_User");
        });

        modelBuilder.Entity<Sys_FunctionPermission>(entity =>
        {
            entity.ToTable("Sys_FunctionPermission", tb => tb.HasComment("系統功能操作權限資料表"));

            entity.Property(e => e.FunctionPermissionId).HasComment("功能操作權限 ID");
            entity.Property(e => e.ParentFunctionPermissionId).HasComment("上層功能操作權限 ID");
            entity.Property(e => e.PermissionKey).HasComment("權限鍵值");
            entity.Property(e => e.FunctionCode).HasComment("功能代碼");
            entity.Property(e => e.FunctionName).HasComment("功能名稱");
            entity.Property(e => e.OperationCode).HasComment("操作代碼");
            entity.Property(e => e.OperationName).HasDefaultValue(string.Empty).HasComment("操作名稱");
            entity.Property(e => e.SortOrder).HasDefaultValue(0).HasComment("排序值");
            entity.Property(e => e.IsEnable).HasDefaultValue(true).HasComment("啟用狀態");
            entity.Property(e => e.CreatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("建立時間");
            entity.Property(e => e.CreatedId).HasComment("建立人員");
            entity.Property(e => e.UpdatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("更新時間");
            entity.Property(e => e.UpdatedId).HasComment("更新人員");

            entity.HasOne<Sys_FunctionPermission>()
                .WithMany()
                .HasForeignKey(e => e.ParentFunctionPermissionId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Sys_FunctionPermission_Parent");
        });

        modelBuilder.Entity<Sys_RoleGroupFunctionPermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleGroupId, e.FunctionPermissionId })
                .HasName("PK_Sys_RoleGroupFunctionPermission");

            entity.ToTable("Sys_RoleGroupFunctionPermission", tb => tb.HasComment("角色群組功能操作權限對應表"));

            entity.Property(e => e.RoleGroupId).HasComment("角色群組 ID");
            entity.Property(e => e.FunctionPermissionId).HasComment("功能操作權限 ID");
            entity.Property(e => e.CreatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("建立時間");
            entity.Property(e => e.CreatedId).HasComment("建立人員");

            entity.HasOne(e => e.RoleGroup)
                .WithMany(e => e.Sys_RoleGroupFunctionPermissions)
                .HasForeignKey(e => e.RoleGroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Sys_RoleGroupFunctionPermission_RoleGroup");

            entity.HasOne(e => e.FunctionPermission)
                .WithMany(e => e.Sys_RoleGroupFunctionPermissions)
                .HasForeignKey(e => e.FunctionPermissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_Sys_RoleGroupFunctionPermission_FunctionPermission");
        });

        modelBuilder.Entity<Sys_VirtualFolder>(entity =>
        {
            entity.ToTable("Sys_VirtualFolder", tb => tb.HasComment("檔案虛擬資料夾資料表"));

            entity.Property(e => e.Id).HasComment("主鍵");
            entity.Property(e => e.Scope).HasComment("範圍：1 Personal、2 Admin");
            entity.Property(e => e.OwnerUserId).HasComment("擁有者使用者帳號");
            entity.Property(e => e.FolderName).HasComment("資料夾名稱");
            entity.Property(e => e.FolderPath).HasComment("虛擬資料夾完整路徑");
            entity.Property(e => e.ParentFolderId).HasComment("父層資料夾 ID");
            entity.Property(e => e.SortOrder).HasDefaultValue(0).HasComment("同層排序值");
            entity.Property(e => e.IsEnable).HasDefaultValue(true).HasComment("啟用狀態");
            entity.Property(e => e.CreatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("建立時間");
            entity.Property(e => e.CreatedId).HasComment("建立人員");
            entity.Property(e => e.UpdatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("更新時間");
            entity.Property(e => e.UpdatedId).HasComment("更新人員");

            entity.HasOne(e => e.ParentFolder)
                .WithMany(e => e.InverseParentFolder)
                .HasForeignKey(e => e.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Sys_VirtualFolder_Parent");
        });

        modelBuilder.Entity<Sys_Attachment>(entity =>
        {
            entity.ToTable("Sys_Attachment", tb => tb.HasComment("檔案主檔資料表"));

            entity.Property(e => e.Id).HasComment("主鍵");
            entity.Property(e => e.FileId).HasDefaultValueSql("(newid())").HasComment("對外檔案識別碼");
            entity.Property(e => e.Scope).HasComment("範圍：1 Personal、2 Admin");
            entity.Property(e => e.OwnerUserId).HasComment("擁有者使用者帳號");
            entity.Property(e => e.FileName).HasComment("原始檔名");
            entity.Property(e => e.Extension).HasDefaultValue(string.Empty).HasComment("副檔名（不含 .）");
            entity.Property(e => e.ContentType).HasComment("MIME 類型");
            entity.Property(e => e.SizeBytes).HasComment("檔案大小（Bytes）");
            entity.Property(e => e.VirtualFolderId).HasComment("虛擬資料夾 ID");
            entity.Property(e => e.FolderPath).HasDefaultValue("/").HasComment("虛擬資料夾路徑快照");
            entity.Property(e => e.StorageProvider).HasComment("儲存供應商名稱");
            entity.Property(e => e.StorageKey).HasComment("儲存鍵值（供應商端路徑/Key）");
            entity.Property(e => e.UploadMode).HasComment("上傳模式：1 Single、2 Chunk");
            entity.Property(e => e.UploadStatus).HasDefaultValue(1).HasComment("上傳狀態：1 Pending、2 Uploading、3 Ready、4 Failed、5 Deleted");
            entity.Property(e => e.IsChunked).HasDefaultValue(false).HasComment("是否為 chunk 上傳");
            entity.Property(e => e.ChunkCount).HasDefaultValue(0).HasComment("chunk 數量");
            entity.Property(e => e.MetadataJson).HasDefaultValue("{}").HasComment("額外資訊 JSON");
            entity.Property(e => e.CreatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("建立時間");
            entity.Property(e => e.CreatedId).HasComment("建立人員");
            entity.Property(e => e.UpdatedTime).HasDefaultValueSql("(sysutcdatetime())").HasComment("更新時間");
            entity.Property(e => e.UpdatedId).HasComment("更新人員");

            entity.HasOne(e => e.VirtualFolder)
                .WithMany()
                .HasForeignKey(e => e.VirtualFolderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_Sys_Attachment_VirtualFolder");
        });

        modelBuilder.ApplyUtcDateTimeConverter();
    }
}
