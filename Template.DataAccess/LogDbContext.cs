using Microsoft.EntityFrameworkCore;
using Template.DataAccess.Extensions;

namespace Template.DataAccess.LogDbContext;

public partial class LogDbContext
{
    public virtual DbSet<UserOperationLog> UserOperationLogs { get; set; }

    public virtual DbSet<QueueLog> QueueLogs { get; set; }

    public virtual DbSet<SsoLog> SsoLogs { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyUtcDateTimeConverter();

        modelBuilder.Entity<UserOperationLog>(entity =>
        {
            entity.ToTable("UserOperationLog", tb => tb.HasComment("使用者操作日誌"));

            entity.Property(e => e.Id).HasComment("使用者操作日誌流水號");
            entity.Property(e => e.EventTime).HasComment("事件發生時間（UTC）");
            entity.Property(e => e.UserId).HasComment("執行使用者代碼");
            entity.Property(e => e.Module).HasComment("功能模組名稱");
            entity.Property(e => e.Action).HasComment("操作種類，對應 AuditActionEnum");
            entity.Property(e => e.Result).HasComment("執行結果，對應 AuditResultEnum");
            entity.Property(e => e.TargetType).HasComment("被操作資料類型");
            entity.Property(e => e.TargetId).HasComment("被操作資料主鍵或外部識別碼");
            entity.Property(e => e.IpAddress).HasComment("呼叫來源 IP");
            entity.Property(e => e.TraceId).HasComment("追蹤識別碼");
            entity.Property(e => e.Message).HasComment("操作訊息");
            entity.Property(e => e.OldValueJson).HasComment("異動前資料 JSON");
            entity.Property(e => e.NewValueJson).HasComment("異動後資料 JSON");
            entity.Property(e => e.MetadataJson).HasComment("額外補充資訊 JSON");
        });

        modelBuilder.Entity<QueueLog>(entity =>
        {
            entity.ToTable("QueueLog", tb => tb.HasComment("佇列執行日誌"));
            entity.Property(e => e.OperatorId).HasComment("操作者或背景工作執行者");
            entity.Property(e => e.JobId).HasComment("背景工作 ID");
            entity.Property(e => e.WorkType).HasComment("工作種類");
            entity.Property(e => e.WorkKey).HasComment("工作識別鍵");
            entity.Property(e => e.EventName).HasComment("佇列事件名稱");
            entity.Property(e => e.Status).HasComment("工作狀態");
            entity.Property(e => e.RetryCount).HasComment("目前重試次數");
            entity.Property(e => e.Message).HasComment("日誌訊息");
            entity.Property(e => e.ErrorMessage).HasComment("錯誤訊息");
            entity.Property(e => e.MetadataJson).HasComment("額外補充資訊 JSON");
        });

        modelBuilder.Entity<SsoLog>(entity =>
        {
            entity.ToTable("SsoLog", tb => tb.HasComment("SSO 串接日誌"));
            entity.Property(e => e.OperatorId).HasComment("操作者；外部系統流程通常等同 ClientId");
            entity.Property(e => e.ClientId).HasComment("SSO ClientId");
            entity.Property(e => e.EventName).HasComment("SSO 事件名稱");
            entity.Property(e => e.Result).HasComment("執行結果");
            entity.Property(e => e.IpAddress).HasComment("呼叫來源 IP");
            entity.Property(e => e.Message).HasComment("日誌訊息");
            entity.Property(e => e.MetadataJson).HasComment("額外補充資訊 JSON");
        });
    }
}
