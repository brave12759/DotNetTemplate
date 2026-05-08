# JWT 驗證說明文件

[← 返回 README](../../../README.md) ｜ [← 返回 Template.WebApi](../../README.md)

## 概述

JWT（JSON Web Token）是本專案唯一的身份驗證機制，所有受保護 API 均需於 HTTP Header 提供有效 Bearer Token。

---

## 架構位置

```
Template.Common/
├── Models/
│   └── CurrentUser.cs                 # 解析後的使用者資訊
├── Services/
│   ├── IJwtService.cs                 # Token 產生介面
│   ├── ICurrentUserService.cs         # 當前使用者介面
│   └── ITokenRevocationService.cs     # Token 撤銷介面
└── Settings/
    └── JwtSettings.cs                 # JWT 設定模型

Template.WebApi/
└── Services/
    ├── JwtService.cs                  # Token 產生實作
    ├── CurrentUserService.cs          # 從 HTTP Claims 解析使用者
    ├── InMemoryTokenRevocationService.cs  # 記憶體撤銷（開發/單機）
    └── SqlServerTokenRevocationService.cs # SQL Server 撤銷（多節點）
```

---

## 設定說明（appsettings.json）

```json
"JwtSettings": {
  "SecretKey": "",          // HMAC-SHA256 簽章金鑰（至少 32 字元，生產環境由環境變數注入）
  "Issuer": "Template",     // 核發者
  "Audience": "Template",   // 受眾
  "ExpiresMinutes": 60      // Token 有效分鐘數
}
```

> 安全提醒：SecretKey 請勿寫入版本控制，應透過環境變數或 Secrets Manager 注入。

---

## Token Claims 對照表

| Claim 類型 | Claim 名稱 | CurrentUser 欄位 | 說明 |
|---|---|---|---|
| ClaimTypes.Name | http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name | UserId | 使用者帳號 |
| ClaimTypes.Email | http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress | Email | 電子郵件 |
| ClaimTypes.MobilePhone | http://schemas.xmlsoap.org/ws/2005/05/identity/claims/mobilephone | MobilePhone | 聯絡電話 |
| dept_id | dept_id | DeptId | 部門 ID |
| ip | ip | Ip | 登入來源 IP |
| jti | jti | TokenId | Token 唯一識別碼（用於登出撤銷） |
| iat | iat | IssuedTime | 簽發時間（Unix Timestamp） |
| exp | exp | ExpiredTime | 到期時間（Unix Timestamp） |

---

## Token 產生流程

```
POST /Auth/Login
  → LoginService.LoginAsync
    → 驗帳號密碼
    → JwtService.GenerateToken(userId, email, mobilePhone, deptId, ip)
      → 組建 Claims 陣列
      → 建立 JwtSecurityToken（HMAC-SHA256）
      → 回傳 Base64url 字串 Token
  → 回應 { Token }
```

---

## Token 驗證流程（每次 API 請求）

```
HTTP Header: Authorization: Bearer <token>
  → UseAuthentication
    → JwtBearerHandler 驗證
      → 簽章 / Issuer / Audience / Lifetime 驗證
      → OnTokenValidated 事件
        → ITokenRevocationService.IsRevoked(jti)   ← 撤銷檢查
        → 若已撤銷 → context.Fail → 401
      → 驗證通過 → 設定 HttpContext.User
  → CurrentUserService.CurrentUser （Lazy 解析 Claims → CurrentUser）
```

---

## Token 登出撤銷

```
POST /Auth/Logout
  → 取出 CurrentUser.TokenId（jti）與 ExpiredTime（exp）
  → LoginService.LogoutAsync(tokenId, expiredUnixTimeSeconds)
    → ITokenRevocationService.Revoke(jti, exp)
      → 寫入撤銷清單（直到 exp 為止）
  → 後續相同 jti 的請求 → OnTokenValidated 拒絕 → 401
```

### 撤銷儲存策略

| 實作 | 適用情境 |
|---|---|
| InMemoryTokenRevocationService | 開發環境 / 單機 / 無 LogConnectionString |
| SqlServerTokenRevocationService | 正式環境 / 多節點部署（共用 Log DB） |

Program.cs 自動選擇：`LogConnectionString` 有值則使用 SQL Server，否則使用記憶體。

---

## 於 Controller 取得當前使用者

在任何繼承 `AuthenticationController`（或自行注入 `ICurrentUserService`）的 Controller 中：

```csharp
var user = _currentUserService.CurrentUser;
// user.UserId, user.Email, user.MobilePhone, user.DeptId, user.Ip, ...
```

亦可透過 `BaseService.CurrentUserService`（BusinessRule 層）直接取用。

---

## DI 註冊（Program.cs）

```csharp
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSingleton<ITokenRevocationService, SqlServerTokenRevocationService>();
// 或 InMemoryTokenRevocationService（依 LogConnectionString 自動切換）
```
