# DataAccess 說明文件

[← 返回方案 README](../../README.md) ｜ [← 返回上層 README](../README.md)

## 概述

`Template.DataAccess` 專案提供 EF Core 資料存取基礎設施，包含兩個 DbContext、UTC 日期時間轉換器、以及相關擴充方法。

---

## 架構位置

```
Template.DataAccess/
├── ProjectDbContext.cs                    # 主業務 DbContext
├── LogDbContext.cs                        # 日誌 DbContext（含 TokenRevocation）
├── Models/
│   └── TokenRevocation.cs                # JWT 撤銷紀錄實體
├── Converters/
│   └── UtcDateTimeConverter.cs           # EF Core DateTime UTC 值轉換器
└── Extensions/
    └── ModelBuilderExtensions.cs         # 全域套用 UTC 轉換器的擴充方法
```

---

## DbContext 說明

### ProjectDbContext

主業務資料庫，對應 `DatabaseSettings.ProjectConnectionString`。

- 包含所有業務實體（Sys_UserInfo 等）
- `OnModelCreating` 呼叫 `OnModelCreatingPartial`（供反向工程覆寫）
- 最後呼叫 `ApplyUtcDateTimeConverter()` 統一套用 UTC 轉換

### LogDbContext

日誌資料庫，對應 `DatabaseSettings.LogConnectionString`。

- 包含 `TokenRevocations`（`DbSet<TokenRevocation>`）
- `OnModelCreating` 設定 TokenRevocation 的 PK、MaxLength、Index
- 最後呼叫 `ApplyUtcDateTimeConverter()`

---

## UtcDateTimeConverter

**位置**：`Converters/UtcDateTimeConverter.cs`

EF Core `ValueConverter<DateTime, DateTime>`，確保所有 DateTime 值在寫入與讀取時的 Kind 正確。

| 方向 | 行為 |
|---|---|
| 寫入 DB（C# → SQL） | Kind=Utc → 原樣寫入；Kind=Local → 轉為 UTC；Kind=Unspecified → 視為 UTC |
| 讀出 DB（SQL → C#） | 強制標記 `DateTimeKind.Utc`，避免後續時區轉換錯誤 |

### 為什麼需要這個？

SQL Server 的 `datetime2` 不儲存時區資訊，EF Core 讀出時預設為 `Unspecified`。若不套用轉換器，在進行時區換算（如顯示台北時間）時會因 Kind=Unspecified 拋出例外或得到錯誤結果。

---

## ModelBuilderExtensions

**位置**：`Extensions/ModelBuilderExtensions.cs`

提供 `ApplyUtcDateTimeConverter(this ModelBuilder)` 擴充方法，在 `OnModelCreating` 結尾呼叫一次，即可對所有實體的 `DateTime` 與 `DateTime?` 屬性套用 `UtcDateTimeConverter`。

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... 其他設定 ...
    modelBuilder.ApplyUtcDateTimeConverter();   // 放在最後
}
```

---

## 反向工程注意事項

使用 EF Core Power Tools 進行反向工程時，產出的檔案會放在以 DbContext 命名的子資料夾（如 `LogDbContext/`）。若已有既有的 DbContext 主檔，反向工程產出的 Context 檔案與既有版本會衝突：

1. 刪除反向工程產出的 `LogDbContext/LogDbContext.cs`（重複的 Context 定義）
2. 刪除反向工程產出的實體（若已在 `Models/` 下有手動維護版本）
3. 保留反向工程在對應 `DbContext.cs`（`partial class`）中的 `OnModelCreatingPartial` 設定
