# LoginService 登入服務說明文件

[← 返回 README](../../../README.md) ｜ [← 返回 BusinessRule](../../README.md)

## 概述

LoginService 位於 Template.BusinessRule 層，負責：

- 帳號密碼驗證
- JWT 發放
- JWT 登出撤銷（依 jti 黑名單）
- 開發環境 DevBypass 相容行為

---

## 架構位置

```text
Template.Common/
├── Models/
│   └── LoginResult.cs                           # 登入操作結果
└── Services/
    ├── ILoginService.cs                          # 登入服務介面（本服務契約）
    ├── IJwtService.cs                            # JWT 產生介面
    ├── ICurrentUserService.cs                    # 當前使用者介面
    └── ITokenRevocationService.cs               # Token 撤銷介面

Template.BusinessRule/
├── BaseService.cs                               # 邏輯層基底（提供 Db / LogDb / CurrentUserService）
└── LoginService/
    ├── Doc/
    │   └── LoginService.md
    └── Services/
        └── LoginService.cs                      # 登入服務實作

Template.WebApi/
├── Controllers/
│   └── AuthController.cs
├── Services/
│   ├── JwtService.cs
│   └── CurrentUserService.cs
└── Authentication/
    └── DevBypassAuthenticationHandler.cs
```

---

## 服務介面

`ILoginService`（`Template.Common.Services`）提供：

- Task<LoginResult> LoginAsync(string userId, string password, string ip)
- Task<LoginResult> DevLoginAsync(string userId, string ip)
- Task LogoutAsync(string tokenId, long expiredUnixTimeSeconds)

---

## 流程說明

### 1) LoginAsync

1. 依 userId 查詢 Sys_UserInfos。
2. 若不存在或停用，回傳失敗。
3. 以 ICryptographyService.VerifyHash 比對密碼。
4. 驗證成功後呼叫 IJwtService.GenerateToken。
5. 回傳 LoginResult.Ok(token)。

### 2) LogoutAsync

1. 接收目前 JWT 的 jti 與 exp。
2. 呼叫 ITokenRevocationService.Revoke(jti, exp)。
3. 之後相同 jti 的 Token，在 JWT 驗證階段會被拒絕。

### 3) JWT 驗證撤銷檢查

在 Program.cs 的 AddJwtBearer 事件中：

- OnTokenValidated 會取出 jti。
- 若 ITokenRevocationService.IsRevoked(jti) 為 true，直接 context.Fail。

---

## API 端點

AuthController 路由為 [controller]/[action]：

- POST /Auth/Login
  - AllowAnonymous
  - 請求：{ UserId, Password }
  - 回應：{ Token }

- POST /Auth/Logout
  - Authorize
  - 需帶 Authorization: Bearer <token>
  - 回應：{ Message: "登出成功。" }

---

## Development 環境行為

系統已啟用 DevBypassAuthenticationHandler：

- 無 Bearer Token：自動以 DevBypassUser 假用戶通過驗證。
- 有 Bearer Token：仍走真實 JWT 驗證與撤銷檢查。

注意：

- /Auth/Logout 需要 Bearer Token 才能撤銷目前 Token。
- 若是 DevBypass 無 Token 直接呼叫 /Auth/Logout，會回傳 BadRequest。

---

## 撤銷儲存策略（多 API 部署）

預設採用 `EfCoreTokenRevocationService`（需設定 `LogConnectionString`）：

- 撤銷資料表：`dbo.TokenRevocation`
- 連線來源：`DatabaseSettings.LogConnectionString`
- 共用方式：多台 API 連到同一個 Log DB，即可共享登出狀態

降級機制：

- 若 `LogConnectionString` 為空，系統自動降級為 `InMemoryTokenRevocationService`。
- 降級模式僅適合單機開發，不適用多節點正式環境。

---

## DI 註冊（Program.cs）

```csharp
// Token 撤銷（自動依 LogConnectionString 選擇）
if (!string.IsNullOrWhiteSpace(databaseSettings.LogConnectionString))
    builder.Services.AddScoped<ITokenRevocationService, EfCoreTokenRevocationService>();
else
    builder.Services.AddSingleton<ITokenRevocationService, InMemoryTokenRevocationService>();

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ILoginService, LoginService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
```
