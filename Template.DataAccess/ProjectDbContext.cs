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

            entity.HasOne<Sys_MenuTree>()
                .WithMany()
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

        modelBuilder.ApplyUtcDateTimeConverter();
    }
}
