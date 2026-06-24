# JwtService

[根目錄 README](../../../README.md) / [WebApi README](../../README.md)

`JwtService` 是 WebApi 層的 `IJwtService` 實作，負責產生個人登入 Token、SSO Server Token、驗證 Bearer Token、檢查 Token 撤銷狀態，以及提供系統管理員維護 JWT 設定。

## 設定來源

JWT 設定改由資料庫 `Sys_BasicSettings` 管理，`Type = JwtSetting`。

| Key | 說明 | 單位 |
|---|---|---|
| `SecretKey` | HMAC-SHA256 簽章金鑰，至少 32 bytes | 文字 |
| `Issuer` | Token 發行者 | 文字 |
| `Audience` | Token 接收對象 | 文字 |
| `PersonalTokenExpire` | 個人登入 Token 有效時間 | 分鐘 |
| `ServerTokenExpire` | SSO Server Token 有效時間 | 秒 |

初始資料可執行 `Template.DataAccess/Scripts/SeedJwtSettings.sql`。部署後可透過 `JwtSettingController` 查詢或更新設定。

`GET /JwtSetting` 只會回傳遮罩後的簽章金鑰；完整 `SecretKey` 只允許透過 `PUT /JwtSetting` 寫入，不會完整回傳給 API 呼叫端。

JWT 設定不再放在 `appsettings.json` 或 `.env`。

`JwtSettingController` 需要 `System.JwtSetting:Manage` 功能權限。權限建立 SQL 已整理在 [SsoService.md](../../../Template.BusinessRule/SsoService/Doc/SsoService.md) 的「既有資料庫啟用 SSO」章節，建立後請指派給正式環境的系統管理員角色群組。

## Token 類型

| 類型 | Claim | 產生方法 | 用途 |
|---|---|---|---|
| 個人 Token | `token_type = personal` | `GeneratePersonalTokenAsync` | 一般使用者登入與呼叫 WebApi |
| Server Token | `token_type = server` | `GenerateServerTokenAsync` | SSO client 登入與系統間 Token 驗證 |

## 個人 Token Claims

| Claim | 來源 |
|---|---|
| `ClaimTypes.Name` | 使用者帳號 |
| `ClaimTypes.Email` | 使用者 Email |
| `ClaimTypes.MobilePhone` | 使用者手機 |
| `dept_id` | 使用者部門 ID |
| `ip` | 登入 IP |
| `jti` | Token ID |
| `iat` | 發行時間 Unix timestamp |
| `exp` | 到期時間 Unix timestamp |
| `role_groups` | 選擇性角色群組 JSON |
| `function_permissions` | 選擇性功能權限樹 JSON |

## 驗證流程

```text
HTTP Authorization: Bearer <token>
  -> JwtBearer middleware 先解析 Token
  -> OnTokenValidated 呼叫 IJwtService.ValidateTokenAsync
  -> JwtService 重新讀取資料庫 JWT 設定
  -> 驗證簽章、Issuer、Audience、有效期限
  -> 依 jti 檢查 Token 是否已撤銷
  -> 將驗證後的 Principal 設回 HttpContext.User
```

## SSO 流程

```text
POST /Sso/login
  -> SsoService 驗證 Sso_Client 中的 ClientId 與 ClientSecret
  -> JwtService.GenerateServerTokenAsync 發出短效 Server Token

POST /Sso/validate-token
  -> SsoService 透過 JwtService 驗證 Token
  -> token_type 必須是 server
  -> client_id 必須對應到啟用中的 SSO client
  -> 回傳 Token 是否有效、ClientId 與 ExpiresAt
```

SSO client secret 存在 `Sso_Client`，不放在 `Sys_BasicSettings`。

## DI

```csharp
builder.Services.AddScoped<IJwtService, JwtService>();
```

`JwtService` 依賴 `ProjectDbContext` 與 `ITokenRevocationService`。
