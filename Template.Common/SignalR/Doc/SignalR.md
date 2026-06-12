# SignalR Queue Infrastructure

[← 返回方案 README](../../../README.md) ｜ [← 返回上層 README](../../README.md)

本專案的 SignalR 推播基礎設施預設搭配 Background Queue 使用。API 或業務服務不直接呼叫 Hub，而是透過 `ISignalRQueueService` 將推播訊息寫入 `Sys_BackgroundJob`，再由背景 worker 執行 `QueuedSignalRMessageHandler` 送到 SignalR client。

這個設計讓推播具備既有 Queue 的重試、鎖定、延遲排程與監控能力，也避免 API request 因即時連線狀態而被拖慢。

## 元件

| 元件 | 路徑 | 說明 |
|---|---|---|
| `ISignalRQueueService` | `Template.Common/SignalR/ISignalRQueueService.cs` | 業務端使用的推播佇列介面 |
| `SignalRQueuedMessage` | `Template.Common/SignalR/SignalRQueuedMessage.cs` | 寫入 Queue 的推播 payload |
| `SignalRTargetType` | `Template.Common/SignalR/SignalRTargetType.cs` | All / Group / User / Connection |
| `SignalRClientMethods` | `Template.Common/SignalR/SignalRClientMethods.cs` | 預設 client method 名稱 |
| `SignalRQueueService` | `Template.BusinessRule/SignalR/Services/SignalRQueueService.cs` | 將訊息序列化並 enqueue |
| `NotificationHub` | `Template.WebApi/Hubs/NotificationHub.cs` | 通用通知 Hub，支援加入與離開群組 |
| `QueuedSignalRMessageHandler` | `Template.WebApi/SignalR/QueuedSignalRMessageHandler.cs` | Background Queue handler，負責實際推播 |
| `SignalRInfrastructureExtensions` | `Template.WebApi/Extensions/SignalRInfrastructureExtensions.cs` | WebApi 端 SignalR 基礎設施預設註冊 |

## 預設註冊

BusinessRule 預設註冊 Queue 與 SignalR queue service：

```csharp
builder.Services.AddBusinessRuleServices(databaseSettings, backgroundQueueSettings);
```

WebApi 預設註冊 SignalR Hub、Queue handler，並掛載 Hub endpoint：

```csharp
builder.Services.AddSignalRInfrastructure();

app.MapSignalRInfrastructure();
```

Hub endpoint：

```text
/hubs/notifications
```

JWT WebSocket 連線可使用 query string 帶入 token：

```text
/hubs/notifications?access_token={jwt}
```

## WorkType

SignalR 推播使用既有 Background Queue：

| 值 | WorkType | 說明 |
|---|---|---|
| 5 | `SignalRMessage` | SignalR 推播訊息 |

如果要調整 worker 數量或 polling interval，可在 `BackgroundQueueSettings.Workers` 加入 `WorkType = 5` 的設定。

```json
"BackgroundQueueSettings": {
  "Enabled": true,
  "Workers": [
    { "WorkType": 5, "WorkerCount": 2, "PollingIntervalSeconds": 1 }
  ]
}
```

## Server 使用方式

業務服務或 Controller 注入 `ISignalRQueueService`：

```csharp
using Template.Common.SignalR;

public class ReportService(ISignalRQueueService signalRQueueService)
{
    public Task NotifyReportCompletedAsync(long reportId, CancellationToken cancellationToken)
    {
        return signalRQueueService.QueueGroupAsync(
            groupName: "report-admins",
            method: SignalRClientMethods.Notification,
            payload: new
            {
                ReportId = reportId,
                Status = "Completed"
            },
            cancellationToken: cancellationToken);
    }
}
```

支援的目標：

| 方法 | 目標 |
|---|---|
| `QueueAllAsync` | 所有連線 |
| `QueueGroupAsync` | 指定群組 |
| `QueueUserAsync` | 指定 SignalR user identifier |
| `QueueConnectionAsync` | 指定 connection id |

## Client 使用方式

JavaScript client 範例：

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/notifications", {
    accessTokenFactory: () => token
  })
  .withAutomaticReconnect()
  .build();

connection.on("Notification", message => {
  console.log("notification", message);
});

await connection.start();
await connection.invoke("JoinGroup", "report-admins");
```

## 注意事項

- `NotificationHub` 有 `[Authorize]`，client 需要有效 JWT，Development 環境仍可走既有 DevBypass 驗證設定。
- `QueueUserAsync` 依賴 SignalR 的 user identifier，目前會使用 ASP.NET Core SignalR 預設的 `ClaimTypes.NameIdentifier`；若 JWT 沒有該 claim，請補 `IUserIdProvider`。
- 推播 payload 會被序列化成 JSON 存入 Queue，請避免放入無法序列化或過大的物件。
- 推播失敗會走既有 Background Queue retry 流程，最終可透過 `/BackgroundQueue/*` API 查詢。

## 測試

SignalR 相關測試位於：

```text
Template.Test/Tests/SignalRQueueServiceTests.cs
```

覆蓋範圍：

- `ISignalRQueueService` 會產生 `BackgroundWorkType.SignalRMessage` job。
- All / Group / User / Connection 的 handler dispatch 路徑。
- Hub `JoinGroup` / `LeaveGroup` 會 trim group name 並呼叫 group manager。
- 無效 target 或 payload 會丟出例外，交由 Queue retry 或失敗狀態記錄。
