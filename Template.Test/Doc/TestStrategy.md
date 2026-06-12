# 測試策略（Core Logic First）

[← 返回方案 README](../../README.md) ｜ [← 返回上層 README](../README.md)

## 目標

本專案測試採「**核心邏輯優先**」原則：

- 先驗證商業規則與資料轉換是否正確
- 避免投入高成本但低回報的測試
- 讓測試可維護、可快速執行、可穩定重現

---

## 測試範圍（有做）

| 範圍 | 內容 |
|---|---|
| `UserService` | CRUD、篩選查詢、重設密碼、重複帳號防呆、參數驗證 |
| `CryptographyService` | AES/RSA round-trip、簽章驗章、PBKDF2 hash/verify、關鍵錯誤路徑 |
| `InMemoryTokenRevocationService` | Revoke / IsRevoked、過期自動清理、邊界條件 |
| `ResponseMessage<T>` / `LoginResult` | 工廠方法輸出正確性 |
| `EnumExtensions` / `MessageEnum` | Description、值轉換、字串轉換、常用狀態碼映射 |

---

## 刻意不測（效益不足）

| 項目 | 原因 |
|---|---|
| ASP.NET Middleware 細節 | 屬框架責任，單元測試成本高、收益低 |
| Swagger UI 呈現 | 偏整合/手動驗證，非核心商業邏輯 |
| Serilog 實際檔案輸出 | 屬外部 I/O，易受環境影響 |
| DevBypass Handler 全流程 | 屬驗證管線整合情境，建議 E2E 驗證 |
| SQL Server Provider 行為本身 | EF Core/Provider 官方已保證，不重覆驗證 |

---

## 設計原則

- 每個測試只驗證一個行為重點
- 優先使用 in-memory 或 fake 依賴，降低測試脆弱度
- 避免測試 private 實作細節，只測對外可觀察行為
- 失敗訊息以業務可讀為主，便於快速定位

---

## 執行方式

```bash
dotnet test Template.Test/Template.Test.csproj
```

## Coverage

CI uses coverlet to collect test coverage and outputs a Cobertura report for GitLab:

```bash
dotnet test Template.Test/Template.Test.csproj \
  --configuration Release \
  --results-directory Template.Test/TestResults \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=cobertura \
  /p:CoverletOutput=Template.Test/TestResults/coverage/ \
  /p:Exclude="[Template.WebApi]*"
```

GitLab reads the explicit `TOTAL_COVERAGE=xx%` marker emitted by `.gitlab-ci.yml`. The marker is calculated from `Template.Test/TestResults/coverage/coverage.cobertura.xml`, which is also published as a `coverage_report` artifact. Coverage focuses on core logic projects; `Template.WebApi` is excluded so controllers and startup plumbing do not dominate the metric.

## SignalR 測試補充

SignalR 測試使用 fake `IHubContext` / `IHubClients` / `IGroupManager` 驗證 dispatch，不啟動真實 WebSocket 連線；真實連線行為屬於整合測試或 E2E 範圍。

目前覆蓋 `SignalRQueueService` 寫入 `BackgroundWorkType.SignalRMessage`、`QueuedSignalRMessageHandler` 依 All / Group / User / Connection dispatch，以及 `NotificationHub` 的 group join/leave 驗證。
