![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4) ![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Web%20API-512BD4) ![EF Core](https://img.shields.io/badge/EF%20Core-10.0-6DB33F) ![SQL Server](https://img.shields.io/badge/SQL%20Server-EF%20Provider-CC2927) ![OpenAPI](https://img.shields.io/badge/OpenAPI-Swagger-85EA2D) ![JWT](https://img.shields.io/badge/Auth-JWT-000000) ![Serilog](https://img.shields.io/badge/Logging-Serilog-2D5C88) ![Background Queue](https://img.shields.io/badge/Jobs-Background%20Queue-0F766E) ![SignalR](https://img.shields.io/badge/Realtime-SignalR-2563EB) ![Health Checks](https://img.shields.io/badge/Health-Checks-16A34A) ![CORS](https://img.shields.io/badge/CORS-Configurable-7C3AED) ![MSTest](https://img.shields.io/badge/Tests-MSTest-007ACC) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

# Template — ASP.NET Core Web API 專案範本

.NET 10 Web API 基礎範本，內建 JWT 驗證、登入/登出、Token 撤銷、全域例外處理、加解密工具、HTTP/HTTPS 相容、Swagger 文件等完整基礎設施。

---

## 授權

本專案採用 [MIT License](LICENSE)。

---

## DDD 分層架構

本範本採用 **Domain-Driven Design（領域驅動設計）** 的分層架構。

### 為何選擇 DDD 分層？

傳統三層式（Controller → Service → Repository）在小專案足夠用，但隨著功能增加容易出現「邏輯散落各處、難以追蹤依賴」的問題。本範本選用 DDD 分層的核心考量如下：

| 考量 | 說明 |
|---|---|
| ⚡ **快速建置** | 各層職責固定，新功能只需在對應層新增，無需思考「這段邏輯該放哪」 |
| 🪶 **專案輕量化** | 每個專案只包含自己層的內容，不額外引入不需要的套件，保持 csproj 乾淨 |
| 📐 **各層分工明確** | Presentation 只管 HTTP 進出、BusinessRule 只管邏輯規則、DataAccess 只管資料存取，邊界清晰不混用 |
| 📖 **可讀性高** | 看到檔案路徑就知道它的職責，例如 `BusinessRule/UserService/Services/UserService.cs` 不需進檔案即可理解用途 |
| 📚 **學習成本低** | 分層概念與 Java Spring Boot 的 `@Controller / @Service / @Repository` 直接對應，有 Spring 背景的開發者幾乎零適應期 |

> 本範本刻意**不導入完整 DDD 戰術模式**（Aggregate Root、Domain Event、Value Object 等），以降低複雜度。分層架構提供足夠的邊界保護，同時維持 Web API 開發的簡潔節奏。

---

對應 Java Spring Boot 的套件分層概念如下：

| 層次 | 專案 | 對應 Spring | 主要內容 |
|---|---|---|---|
| 🌐 **Presentation** | `Template.WebApi/` | `@Controller` / `@RestController` | Controllers · Filters · JWT Handler · Program.cs |
| ⚙️ **Application / Domain** | `Template.BusinessRule/` | `@Service` | 功能模組 · Service 介面/實作 · 模組專用 Models |
| 📦 **Shared Kernel** | `Template.Common/` | 共用模型 / 設定套件 | 共用 Models · Settings · Enums · Extensions · 跨模組介面 |
| 🗄️ **Infrastructure** | `Template.DataAccess/` (EF Core) | `@Repository` / JPA | ProjectDbContext · LogDbContext · Value Converter |

### 依賴規則（Dependency Rule）

```
Template.WebApi
    → Template.BusinessRule   （功能模組與商業邏輯）
    → Template.Common         （共用模型與設定）

Template.BusinessRule
    → Template.Common         （共用模型與設定）
    → Template.DataAccess     （DbContext / Infrastructure）

Template.DataAccess
    → Template.Common         （DatabaseSettings 等共用設定）

Template.Common
    → 不依賴任何其他專案
```

> **重點：** `Template.WebApi` 不應直接依賴或引用 `Template.DataAccess`。需要 DbContext、健康檢查或資料存取相關註冊時，應由 `Template.BusinessRule` 提供轉接擴充方法，讓 Presentation 層只面向 BusinessRule 與 Common。

> 各功能模組可將自己的 `I*Service`、Models 與實作集中在 `Template.BusinessRule/{ModuleName}` 內；需要跨模組共用時，才放到 `Template.Common`。

### .NET DI 容器 vs Spring IoC 對照

| Spring Boot | .NET（本範本） | 說明 |
|---|---|---|
| `@Component` / `@Service` | `builder.Services.AddScoped<I, Impl>()` | 每個 HTTP 請求一個實例 |
| `@Bean` (Singleton) | `builder.Services.AddSingleton<T>()` | 應用程式生命週期共用 |
| `@Autowired` 建構子注入 | Primary Constructor 參數注入 | `public class Svc(IDep dep)` |
| `application.properties` | `appsettings.json` + `IOptions<T>` | 設定注入至強型別類別 |
| `@ControllerAdvice` | `IExceptionFilter` (`GlobalExceptionLogFilter`) | 全域例外處理 |
| `ResponseBodyAdvice` | `IResultFilter` (`ResponseWrapperFilter`) | 統一包裝回傳格式 |

---

## 方案結構

```
Template.slnx
├── Template.WebApi/                  # Presentation — Controllers / Filters / JWT / Program.cs
├── Template.Common/                  # Shared Kernel — 共用 Models / Settings / Enums
├── Template.BusinessRule/            # Domain/Application — 功能模組 / Service 介面與實作
├── Template.DataAccess/              # Infrastructure — EF Core DbContext / Entities / Value Converters
└── Template.Test/                    # 單元測試（MSTest）
```

### 各專案說明文件

| 專案 | 層 | 說明文件 |
|---|---|---|
| `Template.WebApi/` | Presentation | [Template.WebApi README](Template.WebApi/README.md) |
| `Template.Common/` | Shared Kernel | [Template.Common README](Template.Common/README.md) |
| `Template.BusinessRule/` | Domain/Application | [Template.BusinessRule README](Template.BusinessRule/README.md) |
| `Template.DataAccess/` | Infrastructure | [DataAccess README](Template.DataAccess/README.md) |
| `Template.Test/` | 測試 | [Template.Test README](Template.Test/README.md) |

---

## 專案更名方式

> 以下以 `MyApp` 為目標名稱示範，請依實際情況替換。

### 步驟一：全域文字取代（在 VS Code 使用 Ctrl+Shift+H）

**順序很重要：較長的字串優先取代，避免部分取代導致雙重前綴。**

| 搜尋（完全相符） | 取代為 | 影響範圍 |
|---|---|---|
| `Template.WebApi` | `MyApp.WebApi` | namespace、csproj AssemblyName/RootNamespace |
| `Template.BusinessRule` | `MyApp.BusinessRule` | namespace、csproj |
| `Template.DataAccess` | `MyApp.DataAccess` | namespace、csproj |
| `Template.Common` | `MyApp.Common` | namespace、csproj |
| `Template.Test` | `MyApp.Test` | namespace、csproj |
| `TemplateDb` | `MyAppDb` | appsettings 資料庫名稱 |
| `Template API` | `MyApp API` | appsettings ApiSettings.Name（Swagger 標題） |
| `Template` | `MyApp` | 其餘所有出現處（最後執行） |

> **注意：** 最後才取代純 `Template`，避免將 `Template.WebApi` 變成 `MyApp.WebApi.WebApi`。

---

### 步驟二：重新命名實體資料夾與檔案

```
方案目錄/
├── Template.slnx              → MyApp.slnx
├── Template.WebApi/           → MyApp.WebApi/（資料夾）
│   └── Template.WebApi.csproj → MyApp.WebApi.csproj
├── Template.Common/           → MyApp.Common/
│   └── Template.Common.csproj → MyApp.Common.csproj
├── Template.BusinessRule/     → MyApp.BusinessRule/
│   └── Template.BusinessRule.csproj → MyApp.BusinessRule.csproj
├── Template.DataAccess/       → MyApp.DataAccess/
│   └── Template.DataAccess.csproj → MyApp.DataAccess.csproj
└── Template.Test/             → MyApp.Test/
    └── Template.Test.csproj   → MyApp.Test.csproj
```

---

### 步驟三：更新 `.slnx` 內的專案路徑

用文字編輯器開啟 `MyApp.slnx`，將所有 `Template.WebApi/` 路徑改為 `MyApp.WebApi/` 等對應名稱。

---

### 步驟四：更新以下特定檔案

#### `MyApp.WebApi/web.config` 與 `web.config.example`
```xml
<aspNetCore ... arguments=".\MyApp.WebApi.dll" ...>
```

#### `MyApp.WebApi/appsettings.json` / `appsettings.*.json`
- `DatabaseSettings.ProjectConnectionString` → 資料庫名稱 `MyAppDb`
- `DatabaseSettings.LogConnectionString` → 資料庫名稱 `MyAppLogDb`（若有）
- `ApiSettings.Name` → `MyApp API`

---

### 步驟五：重新產生 UserSecretsId（選用）

`Template.WebApi.csproj` 中有一個 `<UserSecretsId>` 用於本機開發機密設定。更名後若需要隔離，執行：

```bash
dotnet user-secrets init --project MyApp.WebApi/MyApp.WebApi.csproj
```

---

### 步驟六：驗證

```bash
dotnet build MyApp.slnx
```

確認無任何 `error` 輸出即完成。

---

## 功能文件

| 功能 | 說明 | 文件 |
|---|---|---|
| [BusinessRule 邏輯層](#) | BaseService、服務總覽、DI 裝配說明 | [BusinessRule README](Template.BusinessRule/README.md) |
| [JWT 驗證](#jwt-驗證) | Token 產生、Claims、驗證流程、撤銷 | [JwtService.md](Template.WebApi/Services/Doc/JwtService.md) |
| [DevBypass 免登入](#devbypass-開發環境免登入) | Development 環境自動通過驗證 | [DevBypass.md](Template.WebApi/Authentication/Doc/DevBypass.md) |
| [Token 撤銷服務](#token-撤銷服務) | EF Core / In-Memory 兩種實作，多節點共用 | [TokenRevocationService.md](Template.BusinessRule/TokenRevocationService/Doc/TokenRevocationService.md) |
| [登入服務](#登入服務) | 帳號密碼驗證、JWT 發放、登出 | [LoginService.md](Template.BusinessRule/LoginService/Doc/LoginService.md) |
| [使用者管理服務](#使用者管理服務) | 使用者 CRUD、重設 / 修改密碼、篩選查詢 | [UserService.md](Template.BusinessRule/UserService/Doc/UserService.md) |
| [密碼管理](#密碼管理) | 密碼規則驗證、雜湊與比對、DDD 層次定位 | [PasswordManager.md](Template.BusinessRule/PasswordManager/Doc/PasswordManager.md) |
| [加解密服務](#加解密服務) | AES 對稱加解密、RSA 非對稱加解密 | [CryptographyService.md](Template.BusinessRule/CryptographyService/Doc/CryptographyService.md) |
| [Filter 管線](#filter-管線) | 全域例外處理、Response 包裝、執行順序 | [Filters.md](Template.WebApi/Filters/Doc/Filters.md) |
| [測試策略](#測試策略) | 核心邏輯優先、測試範圍與非目標 | [TestStrategy.md](Template.Test/Doc/TestStrategy.md) |
| [ResponseMessage](#responsemessage) | 統一回傳格式、MessageEnum、SkipResponseWrap | [ResponseMessage.md](Template.Common/Models/Doc/ResponseMessage.md) |
| [CurrentUser](#currentuser) | JWT Claims 解析、各層取用方式 | [CurrentUser.md](Template.Common/Models/Doc/CurrentUser.md) |
| [設定類別](#設定類別) | 全部 Settings 類別欄位說明與 appsettings 範例 | [Settings.md](Template.Common/Settings/Doc/Settings.md) |
| [ClockUtil](#clockutil) | 時間日期格式、解析、UTC/本地轉換、DateOnly/TimeOnly 互轉 | [ClockUtil.md](Template.Common/Utils/Doc/ClockUtil.md) |
| [Background Queue](#background-queue) | 背景工作佇列基礎設施、HostedService、DI 使用方式 | [BackgroundQueue.md](Template.Common/BackgroundQueue/Doc/BackgroundQueue.md) |
| [SignalR Queue](#signalr-queue) | SignalR 推播基礎設施，搭配 BackgroundQueue 非同步送出 | [SignalR.md](Template.Common/SignalR/Doc/SignalR.md) |
| [DataAccess](#dataaccess) | DbContext、UtcDateTimeConverter、反向工程說明 | [DataAccess.md](Template.DataAccess/Doc/DataAccess.md) |
| [Health Check](#health-check) | 服務存活與資料庫可連線檢查、K8s probe 整合 | Program.cs `AddHealthChecks` 區塊 |

---

## JWT 驗證

- 簽章演算法：HMAC-SHA256
- Claims：UserId / Email / MobilePhone / DeptId / IP / jti / iat / exp
- 每次請求於 `OnTokenValidated` 事件檢查 jti 是否已撤銷
- 設定區段：`JwtSettings`

→ [完整說明](Template.WebApi/Services/Doc/JwtService.md)

---

## DevBypass 開發環境免登入

- 僅 `Development` 環境啟用
- 無 `Authorization` Header 時自動注入假用戶（由 `DevBypassUser` 設定）
- 有 Bearer Token 時仍走正常 JWT 驗證

→ [完整說明](Template.WebApi/Authentication/Doc/DevBypass.md)

---

## Token 撤銷服務

- `LogConnectionString` 有值 → `EfCoreTokenRevocationService`（Scoped，LINQ，多節點共用）
- `LogConnectionString` 空值 → `InMemoryTokenRevocationService`（Singleton，單機）
- 撤銷資料表：`dbo.TokenRevocation`（由 `LogDbContext` 管理）

→ [完整說明](Template.BusinessRule/TokenRevocationService/Doc/TokenRevocationService.md)

---

## 登入服務

- `POST /Auth/Login`：驗帳號密碼 → 發放 JWT
- `POST /Auth/Logout`：撤銷當前 Token（jti 加入黑名單）
- `DevLoginAsync`：開發用，查不到使用者時以假資料發放 Token

→ [完整說明](Template.BusinessRule/LoginService/Doc/LoginService.md)

---

## 使用者管理服務

- `GET /User/List`：依關鍵字與啟用狀態查詢使用者清單
- `GET /User/GetById`：查單筆使用者
- `POST /User/Create`：建立使用者（密碼雜湊）
- `PUT /User/Update`：更新基本資料（不含密碼）
- `DELETE /User/Delete`：刪除使用者
- `POST /User/ResetPassword`：重設密碼（PBKDF2 雜湊）

→ [完整說明](Template.BusinessRule/UserService/Doc/UserService.md)

---

## 加解密服務

- AES-256 對稱加解密（`SymmetricEncrypt` / `SymmetricDecrypt`）
- RSA 非對稱加解密（`AsymmetricEncrypt` / `AsymmetricDecrypt`）
- `GenerateSymmetricKey` / `GenerateRsaKeyPair` 提供金鑰產生工具

→ [完整說明](Template.BusinessRule/CryptographyService/Doc/CryptographyService.md)

---

## Filter 管線

| 順序 | Filter | 說明 |
|---|---|---|
| 最先 | `GlobalExceptionLogFilter` (Order=int.MinValue) | 捕捉例外，記錄 TraceId/UserId/TokenId，回傳 500 |
| 最後 | `ResponseWrapperFilter` (Order=int.MaxValue) | 包裝所有回傳為 `ResponseMessage<T>` |

→ [完整說明](Template.WebApi/Filters/Doc/Filters.md)

---

## 測試策略

- 單元測試只聚焦核心商業邏輯（帳密規則、使用者 CRUD、Token 撤銷、加解密、通用模型/Enum）。
- 不鑽牛角尖測試框架細節與低效益項目（如 Swagger UI、Serilog 實體檔案輸出、Middleware 呈現細節）。
- 跨層流程與環境依賴行為以整合測試或手動驗證補齊。

→ [完整說明](Template.Test/Doc/TestStrategy.md)

---

## ResponseMessage

所有 API 回傳格式：

```json
{ "Status": 200, "Message": "成功", "Details": { } }
```

→ [完整說明](Template.Common/Models/Doc/ResponseMessage.md)

---

## CurrentUser

從 JWT Claims 解析的當前使用者，可在 Controller 與 BusinessRule 層直接取用。

→ [完整說明](Template.Common/Models/Doc/CurrentUser.md)

---

## 設定類別

| 區段 | 用途 |
|---|---|
| ApiSettings | Swagger 標題 |
| DatabaseSettings | 主 DB / Log DB 連線字串 |
| JwtSettings | JWT 金鑰、Issuer、有效時間 |
| HashSettings | PBKDF2 迭代次數 |
| HttpsSettings | HTTPS / HSTS / PFX 憑證 |
| LogSettings | Serilog 檔案輸出 |
| CryptographyKeySettings | AES / RSA 金鑰 |
| TimeZoneSettings | 應用程式時區（IANA） |

→ [完整說明](Template.Common/Settings/Doc/Settings.md)

---

## ClockUtil

- `DateTime` / `DateTimeOffset` / `DateOnly` / `TimeOnly` 常用轉換
- UTC 與指定時區互轉
- 固定格式解析與輸出
- Unix 秒數 / 毫秒數轉換
- 日期起訖時間工具

→ [完整說明](Template.Common/Utils/Doc/ClockUtil.md)

---

## Background Queue

- 使用資料庫保存工作狀態，可依 `WorkType` 分流
- API 或 Service 可注入 `IBackgroundTaskQueue` 加入背景工作
- Worker 執行時會建立獨立 DI scope，可安全使用 scoped service
- 適合寄信、產報表、匯入檔案、呼叫外部 API 等非即時工作

→ [完整說明](Template.Common/BackgroundQueue/Doc/BackgroundQueue.md)

---

## SignalR Queue

- `ISignalRQueueService` 預設註冊，可將推播訊息寫入 BackgroundQueue。
- `QueuedSignalRMessageHandler` 預設註冊為 `IBackgroundJobHandler`，處理 `BackgroundWorkType.SignalRMessage`。
- Hub endpoint 為 `/hubs/notifications`，使用 JWT 驗證，WebSocket 可透過 `access_token` query string 傳 token。
- 支援 All / Group / User / Connection 四種推播目標。

→ [完整說明](Template.Common/SignalR/Doc/SignalR.md)

---

## DataAccess

- `ProjectDbContext`：主業務資料庫
- `LogDbContext`：日誌資料庫（含 TokenRevocation）
- `UtcDateTimeConverter`：所有 DateTime 自動轉 UTC
- 反向工程後的衝突處理方式

→ [完整說明](Template.DataAccess/Doc/DataAccess.md)

---

## Health Check

### 端點

```
GET /health
```

不需要 JWT，供負載均衡器、K8s probe、監控系統呼叫。

### 回傳結果

| HTTP 狀態碼 | 回應內容 | 說明 |
|---|---|---|
| `200 OK` | `Healthy` | 所有檢查項目通過 |
| `503 Service Unavailable` | `Unhealthy` | 任一項目失敗（如 DB 連線中斷）|

### 檢查項目

| 名稱 | 說明 | 條件 |
|---|---|---|
| `project-db` | 對 `ProjectDbContext` 執行 `CanConnectAsync()` | `ProjectConnectionString` 非空才加入 |
| `log-db` | 對 `LogDbContext` 執行 `CanConnectAsync()` | `LogConnectionString` 非空才加入 |

> 本機開發若連線字串為空（未連真實 DB），健康檢查自動略過 DB 項目，僅回傳 `Healthy`。

### 與 Swagger 的關係

`/health` 是 Minimal API（非 MVC Controller），Swashbuckle 不會自動掃描，**不會出現在 Swagger UI**。直接以瀏覽器或 curl 呼叫即可。

### K8s 設定範例

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 10
  periodSeconds: 30

readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 10
```

