# Template.WebApi — Web API 進入點說明

[← 返回方案 README](../README.md)

本專案為 DDD **Presentation Layer**，負責 HTTP 進入點、身份驗證、MVC Filter 管線、JSON 序列化與 Swagger 文件。所有業務邏輯委派給 `Template.BusinessRule`，不在此層直接操作資料庫。

---

## 架構定位

```
Template.WebApi（本專案 — Presentation Layer）
    → Template.BusinessRule   （功能模組與商業邏輯）
    → Template.Common         （共用模型與設定）
```

`Template.WebApi` 不直接引用 `Template.DataAccess`。資料庫、DbContext、資料庫健康檢查等 Infrastructure 註冊，統一由 `Template.BusinessRule` 的擴充方法轉接。

---

## 目錄結構

```
Template.WebApi/
├── Program.cs                              # DI 裝配、Middleware 管線、Swagger 設定
├── appsettings.json                        # 預設設定（不含機密值）
├── appsettings.Development.json            # 開發環境設定（含 DB 連線字串）
├── appsettings.Production.json             # 正式環境設定（留空，由環境變數填入）
├── web.config                              # IIS 部署設定（Development 環境用）
├── web.config.example                      # IIS 部署設定（Production 範本）
├── Authentication/
│   ├── DevBypassAuthenticationHandler.cs   # 開發環境免登入處理器
│   ├── DevBypassAuthenticationOptions.cs   # DevBypass 選項（無設定值）
│   └── DevBypassUserSettings.cs            # DevBypass 假使用者設定
├── Controllers/
│   ├── BaseController.cs                   # 基底（[ApiController] [Route("[controller]")])
│   ├── AuthenticationController.cs         # 需授權 Controller 基底（[Authorize]）
│   ├── AuthController.cs                   # Login / Refresh / Me / Logout
│   ├── BackgroundQueueController.cs        # /BackgroundQueue/*，背景工作佇列查詢
│   ├── CryptographyController.cs           # POST /Cryptography/*（需 JWT）
│   └── UserController.cs                   # /User/*（CRUD + ResetPassword，需 JWT）
├── Extensions/
│   └── SignalRInfrastructureExtensions.cs  # SignalR Hub + Queue handler 預設註冊
├── Hubs/
│   └── NotificationHub.cs                  # /hubs/notifications
├── SignalR/
│   └── QueuedSignalRMessageHandler.cs      # Queue worker 實際推播到 Hub
├── Converters/
│   └── DateTimeJsonConverter.cs            # DateTime / DateTimeOffset JSON 時區轉換器
├── Filters/
│   ├── GlobalExceptionLogFilter.cs         # 全域例外攔截 + Serilog 記錄（Order=int.MinValue）
│   ├── ResponseWrapperFilter.cs            # 統一回傳包裝為 ResponseMessage<T>（Order=int.MaxValue）
│   └── SkipResponseWrapAttribute.cs        # 標記跳過 ResponseWrapper 的 Attribute
├── Models/
│   └── Auth/
│       ├── LoginRequest.cs                 # POST /Auth/login 請求模型
│       └── AuthTokenResponse.cs            # Login / Refresh Token 回傳模型
├── Services/
│   ├── JwtService.cs                       # JWT Token 產生（實作 IJwtService）
│   └── CurrentUserService.cs              # JWT Claims → CurrentUser（實作 ICurrentUserService）
└── Properties/
    └── launchSettings.json                 # VS 啟動設定（https profile）
```

---

## Controller 繼承結構

```
BaseController<T>
│   [ApiController] [Route("[controller]")]
│
└── AuthenticationController<T>
│       [Authorize]
│
└── CryptographyController          ← 需 JWT（繼承 AuthenticationController）
└── UserController                  ← 需 JWT（繼承 AuthenticationController）

AuthController                      ← 各 Action 自行標記 [AllowAnonymous] / [Authorize]
```

---

## 服務說明文件

| 服務 / 功能 | 說明 | 文件 |
|---|---|---|
| JWT 驗證 | Token 產生、Claims 結構、撤銷檢查流程 | [JwtService.md](Services/Doc/JwtService.md) |
| DevBypass 免登入 | Development 環境自動通過驗證機制 | [DevBypass.md](Authentication/Doc/DevBypass.md) |
| Filter 管線 | 全域例外處理、Response 包裝、執行順序 | [Filters.md](Filters/Doc/Filters.md) |
| Background Queue | 背景資料庫佇列、HostedService 與前端查詢 API | [BackgroundQueue.md](../Template.Common/BackgroundQueue/Doc/BackgroundQueue.md) |
| SignalR Queue | `/hubs/notifications` 與 Queue handler，預設搭配 BackgroundQueue 使用 | [SignalR.md](../Template.Common/SignalR/Doc/SignalR.md) |
| 使用者管理 | 使用者 CRUD、重設密碼、參數驗證規則 | [UserService.md](../Template.BusinessRule/UserService/Doc/UserService.md) |
 
---

## REST 路由與 PATCH

Controller 基底只定義 `[Route("[controller]")]`，各 action 的路由由 `RestfulRouteConvention` 依命名慣例集中產生。

| Action 命名 | HTTP Method | 路由範例 | 用途 |
|---|---|---|---|
| `List` / `Get` | `GET` | `/User` | 查詢集合或單一設定資源 |
| `GetById(id)` | `GET` | `/User/1` | 查詢單筆資料 |
| `Create` | `POST` | `/User` | 建立資料 |
| `Update` | `PUT` | `/User` | 完整更新資源 |
| `Patch(id)` | `PATCH` | `/User/1` | 局部更新資源 |
| `Delete(id)` | `DELETE` | `/User/1` | 刪除資料 |

`PUT` 保留完整更新語意，request body 必須帶完整 `UpdateRequest`。`PATCH` 使用 `Microsoft.AspNetCore.JsonPatch.SystemTextJson`，先讀取現有資料組成 `UpdateRequest`，套用 JSON Patch 後再呼叫既有 `UpdateAsync`，因此驗證、稽核與資料更新仍集中在 Service。

JSON Patch request 範例：

```json
[
  { "op": "replace", "path": "/userName", "value": "Alice Chen" },
  { "op": "replace", "path": "/isEnable", "value": true }
]
```

目前只有單筆資源更新支援 `PATCH`，例如 `User`、`Department`、`MenuTree`、`RoleGroup`、`FunctionPermission` 與 `Sso/clients`。角色權限整批覆蓋、使用者角色群組、JWT 設定等批次或設定型操作仍使用 `PUT`。

---

## 環境變數

`Program.cs` 在綁定設定前會先載入 `Template.WebApi/.env` 與 `Template.WebApi/.env.{Environment}`。實際系統環境變數優先權高於 `.env` 檔，所有環境變數也會覆蓋 `appsettings*.json`。

巢狀設定請使用雙底線表示：

| 設定 | 環境變數 |
|---|---|
| `DatabaseSettings.ProjectConnectionString` | `DatabaseSettings__ProjectConnectionString` |
| `DatabaseSettings.LogConnectionString` | `DatabaseSettings__LogConnectionString` |
| `JwtSettings.SecretKey` | `JwtSettings__SecretKey` |
| `JwtSettings.Issuer` | `JwtSettings__Issuer` |
| `JwtSettings.Audience` | `JwtSettings__Audience` |
| `CryptographyKeySettings.SymmetricKeyBase64` | `CryptographyKeySettings__SymmetricKeyBase64` |
| `CryptographyKeySettings.SymmetricIvBase64` | `CryptographyKeySettings__SymmetricIvBase64` |
| `CryptographyKeySettings.RsaPublicKeyPem` | `CryptographyKeySettings__RsaPublicKeyPem` |
| `CryptographyKeySettings.RsaPrivateKeyPem` | `CryptographyKeySettings__RsaPrivateKeyPem` |

## 日誌 API

| Controller | API | 說明 |
|---|---|---|
| `LogController` | `GET /Log/user-operation-logs` | 查詢使用者操作日誌，需要 `System.UserOperationLog:View` 權限 |
| `LogController` | `GET /Log/queue-logs` | 查詢佇列日誌，需要 `System.QueueLog:View` 權限 |
| `LogController` | `GET /Log/sso-logs` | 查詢 SSO 日誌，需要 `System.SsoLog:View` 權限 |

日誌服務文件請參考 [LogService.md](../Template.BusinessRule/LogService/Doc/LogService.md)。

`appsettings.Development.json` 已提供本機啟動所需的 JWT 基本設定。若需覆寫，可改用 `Template.WebApi/.env.example` 作為 `.env` 或 `.env.Development` 範本；`.env` 檔不會納入 git，且環境變數優先權仍高於 `appsettings*.json`。

JWT 驗證設定目前可由 `JwtSettings` 組態區段或對應環境變數提供；缺少 `SecretKey`、`Issuer`、`Audience` 任一值時，應用程式仍會在啟動階段直接拋出例外並終止。

HTTPS 由 hosting layer 處理。請在 IIS、反向代理或 ingress 綁定 443 與 SSL；API 本身不載入 PFX 憑證，也不強制 HTTPS redirect。
