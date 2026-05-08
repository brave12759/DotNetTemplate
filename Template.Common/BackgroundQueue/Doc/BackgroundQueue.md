# Background Queue 背景資料庫佇列
[回到 Template.Common](../../README.md)

背景佇列採用資料庫保存工作狀態，適合附件上傳、報表產製、m3u8 更新等需要跨行程追蹤進度的工作。佇列類型由 `BackgroundWorkType` enum 控制，資料庫欄位 `WorkType` 存 `INT`，不要直接存字串。

## 結構

| 類型 | 路徑 | 說明 |
|---|---|---|
| 佇列介面 | `Template.Common/BackgroundQueue/IBackgroundTaskQueue.cs` | 加入工作、取得工作、完成、失敗與 pending 統計 |
| 查詢介面 | `Template.Common/BackgroundQueue/IBackgroundJobMonitorService.cs` | 提供前端查詢佇列摘要與工作明細 |
| 工作資料 | `Template.Common/BackgroundQueue/BackgroundJob.cs` | worker 取得後交給 handler 的工作資料 |
| 工作類型 | `Template.Common/BackgroundQueue/BackgroundWorkType.cs` | 控制佇列類型的 enum |
| 工作狀態 | `Template.Common/BackgroundQueue/BackgroundJobStatus.cs` | Pending / Processing / Succeeded / Failed / Canceled |
| 處理器介面 | `Template.Common/BackgroundQueue/IBackgroundJobHandler.cs` | 依 `WorkType` 實作背景工作處理邏輯 |
| DB 佇列實作 | `Template.BusinessRule/BackgroundQueue/Services/DbBackgroundTaskQueue.cs` | 使用 `ProjectDbContext` 存取佇列表 |
| DB 查詢實作 | `Template.BusinessRule/BackgroundQueue/Services/BackgroundJobMonitorService.cs` | 統計與查詢 `Sys_BackgroundJob` |
| EF Entity | `Template.DataAccess/ProjectDbContext/Sys_BackgroundJob.cs` | 背景工作佇列表 Entity |
| HostedService | `Template.BusinessRule/BackgroundQueue/Services/QueuedBackgroundService.cs` | 依 handler 與設定啟動 worker |
| 查詢 API | `Template.WebApi/Controllers/BackgroundQueueController.cs` | 提供前端查詢佇列狀態 |

## 設定

```json
"BackgroundQueueSettings": {
  "Enabled": true,
  "DefaultPollingIntervalSeconds": 5,
  "DefaultLockTimeoutSeconds": 300,
  "DefaultMaxRetryCount": 3,
  "ShutdownTimeoutSeconds": 30,
  "Workers": [
    { "WorkType": 1, "WorkerCount": 2 },
    { "WorkType": 2, "WorkerCount": 1 },
    { "WorkType": 3, "WorkerCount": 1, "PollingIntervalSeconds": 2 }
  ]
}
```

`BackgroundWorkType`：

| 值 | 名稱 | 說明 |
|---|---|---|
| 1 | `AttachmentUpload` | 大型附件上傳 |
| 2 | `Report` | 報表產製 |
| 3 | `M3u8Refresh` | m3u8 播放清單更新 |

`BackgroundJobStatus`：

| 值 | 名稱 | 說明 |
|---|---|---|
| 0 | `Pending` | 等待處理 |
| 1 | `Processing` | 處理中 |
| 2 | `Succeeded` | 已完成 |
| 3 | `Failed` | 已失敗且不再重試 |
| 4 | `Canceled` | 已取消 |

`Workers` 以 `WorkType` 分流。不同工作類型可配置不同 worker 數量，避免大型附件、報表與 m3u8 更新互相占用同一條處理通道。

## 入列與處理

### 1. 加入工作

```csharp
using Template.Common.BackgroundQueue;

public class ReportController(IBackgroundTaskQueue queue) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateReport(CancellationToken cancellationToken)
    {
        await queue.EnqueueAsync(
            workType: BackgroundWorkType.Report,
            payloadJson: """{"ReportId":123}""",
            workKey: "RPT-123",
            cancellationToken: cancellationToken);

        return Accepted();
    }
}
```

### 2. 實作處理器

```csharp
using System.Text.Json;
using Template.Common.BackgroundQueue;

public class ReportJobHandler(IReportService reportService) : IBackgroundJobHandler
{
    public BackgroundWorkType WorkType => BackgroundWorkType.Report;

    public async Task HandleAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<ReportPayload>(job.PayloadJson)
            ?? throw new InvalidOperationException("Report payload 格式錯誤。");

        await reportService.CreateAsync(payload.ReportId, cancellationToken);
    }
}

public record ReportPayload(int ReportId);
```

### 3. 註冊處理器

背景佇列基礎設施已由 `Template.BusinessRule/Extensions/ServiceCollectionExtensions.cs` 統一註冊。各功能自己的 handler 仍需由開發者依專案需求自行註冊：

```csharp
builder.Services.AddScoped<IBackgroundJobHandler, ReportJobHandler>();
```

## 查詢 API

背景佇列提供查詢 API，讓前端可呈現目前有多少工作等待、處理中、完成或失敗。

| Method | 路由 | 說明 |
|---|---|---|
| GET | `/BackgroundQueue/Summary` | 取得總數與各 `WorkType` 統計 |
| GET | `/BackgroundQueue/List?workType=2&status=0&page=1&pageSize=50` | 查詢工作明細清單 |
| GET | `/BackgroundQueue/GetById?id=1` | 查詢單一工作明細 |

`workType` 與 `status` 可不傳。不傳時代表不篩選。`pageSize` 限制為 1 到 200，避免前端一次拉出過多資料。

## MSSQL 建表語法

```sql
CREATE TABLE [dbo].[Sys_BackgroundJob]
(
    [Id] BIGINT IDENTITY(1,1) NOT NULL,
    [WorkType] INT NOT NULL,
    [WorkKey] NVARCHAR(200) NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_WorkKey] DEFAULT (N''),
    [PayloadJson] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_PayloadJson] DEFAULT (N''),
    [Priority] INT NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_Priority] DEFAULT (0),
    [Status] INT NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_Status] DEFAULT (0),
    [RetryCount] INT NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_RetryCount] DEFAULT (0),
    [MaxRetryCount] INT NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_MaxRetryCount] DEFAULT (3),
    [ScheduledTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_ScheduledTime] DEFAULT (SYSUTCDATETIME()),
    [StartedTime] DATETIME2(7) NULL,
    [CompletedTime] DATETIME2(7) NULL,
    [LockedUntil] DATETIME2(7) NULL,
    [LockedBy] NVARCHAR(100) NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_LockedBy] DEFAULT (N''),
    [LastError] NVARCHAR(MAX) NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_LastError] DEFAULT (N''),
    [CreatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_CreatedTime] DEFAULT (SYSUTCDATETIME()),
    [CreatedId] NVARCHAR(50) NOT NULL,
    [UpdatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_UpdatedTime] DEFAULT (SYSUTCDATETIME()),
    [UpdatedId] NVARCHAR(50) NOT NULL,
    [Version] UNIQUEIDENTIFIER NOT NULL CONSTRAINT [DF_Sys_BackgroundJob_Version] DEFAULT (NEWID()),
    CONSTRAINT [PK_Sys_BackgroundJob] PRIMARY KEY CLUSTERED ([Id] ASC)
);

CREATE INDEX [IX_Sys_BackgroundJob_Dequeue]
    ON [dbo].[Sys_BackgroundJob] ([WorkType], [Status], [ScheduledTime], [Priority]);

CREATE INDEX [IX_Sys_BackgroundJob_WorkKey]
    ON [dbo].[Sys_BackgroundJob] ([WorkKey]);

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'背景工作資料庫佇列表',
    @level0type=N'SCHEMA', @level0name=N'dbo',
    @level1type=N'TABLE',  @level1name=N'Sys_BackgroundJob';

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'工作流水號',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'Id';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'工作類型 enum 值：1 AttachmentUpload、2 Report、3 M3u8Refresh',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'WorkType';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'工作識別鍵，可用來查詢同一批工作',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'WorkKey';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'工作參數 JSON',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'PayloadJson';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'優先順序，數字越小越早處理',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'Priority';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'工作狀態：0 Pending、1 Processing、2 Succeeded、3 Failed、4 Canceled',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'Status';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'已重試次數',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'RetryCount';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最大重試次數',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'MaxRetryCount';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'預計執行時間',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'ScheduledTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'開始處理時間',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'StartedTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'完成時間',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'CompletedTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'工作鎖定到期時間',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'LockedUntil';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'處理此工作的 worker 識別',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'LockedBy';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'最後一次錯誤訊息',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'LastError';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'建立時間',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'CreatedTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'建立人員',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'CreatedId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'更新時間',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'UpdatedTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'更新人員',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'UpdatedId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'併發控制版本',
    @level0type=N'SCHEMA', @level0name=N'dbo', @level1type=N'TABLE', @level1name=N'Sys_BackgroundJob', @level2type=N'COLUMN', @level2name=N'Version';
```

## 注意事項

- 這是資料庫佇列，工作狀態會保存於 `Sys_BackgroundJob`。
- `WorkType` 只存 enum 整數值，新增佇列功能時請先更新 `BackgroundWorkType`，再新增對應 handler。
- `PayloadJson` 只保存工作需要的參數，不要放大型檔案內容。
- 前端畫面若要呈現進度，建議用 `/BackgroundQueue/Summary` 做總覽，用 `/BackgroundQueue/List` 顯示各工作明細。
