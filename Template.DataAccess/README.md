# DataAccess — 資料存取層說明

[← 返回方案 README](../README.md)

本專案為 DDD **Infrastructure Layer**，使用 EF Core 10 操作 SQL Server，提供兩個 DbContext、UTC 日期時間值轉換器與 EF Core Power Tools 反向工程整合。

---

## 架構定位

```
Template.BusinessRule   ← 透過 BaseService.Db / LogDb 使用
Template.WebApi         ← DI 注冊 DbContext（Program.cs）
        ↓ 引用
DataAccess（本專案 — Infrastructure Layer）
        依賴 Template.Common（Settings）
```

---

## 目錄結構

```
Template.DataAccess/
├── ProjectDbContext.cs                 # 主業務 DbContext（partial，手動維護）
├── LogDbContext.cs                     # 日誌 DbContext（含 TokenRevocations DbSet）
├── efpt.config.json                    # EF Core Power Tools 反向工程設定
├── Converters/
│   └── UtcDateTimeConverter.cs         # EF Core DateTime → UTC ValueConverter
├── Extensions/
│   └── ModelBuilderExtensions.cs       # ApplyUtcDateTimeConverter() 擴充方法
├── Models/
│   └── TokenRevocation.cs              # JWT 撤銷紀錄實體（dbo.TokenRevocation）
├── ProjectDbContext/                    # EF Core Power Tools 自動產生（partial class）
│   ├── ProjectDbContext.cs              # DbSet 定義 + OnModelCreatingPartial
│   └── Sys_UserInfo.cs                 # 系統使用者資訊 Entity
└── Doc/
    └── DataAccess.md                   # 詳細說明文件
```

---

## 資料庫說明

| DbContext | 對應設定 | 用途 |
|---|---|---|
| `ProjectDbContext` | `DatabaseSettings.ProjectConnectionString` | 主業務資料（Sys_UserInfo 等） |
| `LogDbContext` | `DatabaseSettings.LogConnectionString` | 日誌 + JWT 撤銷紀錄（TokenRevocation） |

### 為何拆分主 DB 與 Log DB？

大多數專案都會有事件查找與稽核需求（操作記錄、Token 撤銷清單、系統事件等）。若將 Log 資料與主業務資料放在同一個資料庫，會產生以下問題：

| 問題 | 說明 |
|---|---|
| **資源競爭** | Log 寫入頻率高（每次請求都可能寫入），會與主業務的讀寫查詢搶占連線、I/O 與鎖定資源，影響主流程回應速度 |
| **資料量膨脹** | Log 資料隨時間累積量龐大，可能拖慢主業務資料表的索引與備份效率 |
| **維護彈性差** | 主 DB 需要嚴謹的備份與還原策略；Log DB 則可採較寬鬆的保留策略（如定期清除過期紀錄），混在一起難以分別管理 |
| **擴展限制** | 未來若需要將 Log 換成專用時序資料庫（如 Elasticsearch、InfluxDB），拆分架構只需替換 `LogDbContext` 的實作，不影響主業務 |

> **簡單說：** 主 DB 負責「現在」的業務狀態；Log DB 負責「歷史」的事件軌跡。兩者讀寫模式不同，分開部署可讓雙方各自調校，互不干擾。

---

## 說明文件

| 主題 | 文件 |
|---|---|
| DbContext 架構、UtcDateTimeConverter 原理、反向工程注意事項 | [DataAccess.md](Doc/DataAccess.md) |
