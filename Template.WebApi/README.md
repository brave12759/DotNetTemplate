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
│   ├── BaseController.cs                   # 基底（[ApiController] [Route("[controller]/[action]")]）
│   ├── AuthenticationController.cs         # 需授權 Controller 基底（[Authorize]）
│   ├── AuthController.cs                   # POST /Auth/Login、POST /Auth/Logout
│   ├── BackgroundQueueController.cs        # /BackgroundQueue/*，背景工作佇列查詢
│   ├── CryptographyController.cs           # POST /Cryptography/*（需 JWT）
│   └── UserController.cs                   # /User/*（CRUD + ResetPassword，需 JWT）
├── Converters/
│   └── DateTimeJsonConverter.cs            # DateTime / DateTimeOffset JSON 時區轉換器
├── Filters/
│   ├── GlobalExceptionLogFilter.cs         # 全域例外攔截 + Serilog 記錄（Order=int.MinValue）
│   ├── ResponseWrapperFilter.cs            # 統一回傳包裝為 ResponseMessage<T>（Order=int.MaxValue）
│   └── SkipResponseWrapAttribute.cs        # 標記跳過 ResponseWrapper 的 Attribute
├── Models/
│   └── Auth/
│       └── LoginRequest.cs                 # POST /Auth/Login 請求模型
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
│   [ApiController] [Route("[controller]/[action]")]
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
| 使用者管理 | 使用者 CRUD、重設密碼、參數驗證規則 | [UserService.md](../Template.BusinessRule/UserService/Doc/UserService.md) |
