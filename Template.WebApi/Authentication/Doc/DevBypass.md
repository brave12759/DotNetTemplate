# DevBypass 開發環境免登入說明文件

[← 返回 README](../../../README.md) ｜ [← 返回 Template.WebApi](../../README.md)

## 概述

DevBypass 是 Development 環境專用的認證機制，讓開發者不需先登入取得 Token，即可直接呼叫所有受保護 API。

---

## 架構位置

```
Template.WebApi/
└── Authentication/
    ├── DevBypassAuthenticationHandler.cs  # 自訂認證 Handler
    ├── DevBypassAuthenticationOptions.cs  # Handler 的空 Options
    └── DevBypassUserSettings.cs           # 假用戶設定模型（由 appsettings 載入）
```

---

## 設定說明（appsettings.Development.json）

```json
"DevBypassUser": {
  "UserId": "dev",
  "Email": "dev@localhost",
  "MobilePhone": "",
  "DeptId": "0"
}
```

這些欄位會對應至 `CurrentUser`，讓 BusinessRule 層呼叫 `CurrentUserService.CurrentUser` 時取得正確的假用戶資訊。

---

## 運作原理

```
HTTP 請求進入
  → UseAuthentication
    → DevBypassAuthenticationHandler.HandleAuthenticateAsync
      ┌─ 有 Authorization: Bearer <token>
      │    → 轉交 JwtBearerHandler 驗證（走真實 JWT 流程）
      └─ 無 Authorization Header
           → 自動建立假用戶 ClaimsIdentity
           → AuthenticateResult.Success
           → 所有 [Authorize] 端點直接通過
```

---

## 假用戶 Claims 內容

DevBypassAuthenticationHandler 建立的假用戶含以下 Claims，與真實 JWT 格式完全一致：

| Claim | 值 |
|---|---|
| ClaimTypes.Name | DevBypassUser.UserId |
| ClaimTypes.Email | DevBypassUser.Email |
| ClaimTypes.MobilePhone | DevBypassUser.MobilePhone |
| dept_id | DevBypassUser.DeptId |
| ip | 127.0.0.1 |
| jti | 每次請求產生新的 GUID |
| iat | 請求當下 UTC Unix Timestamp |
| exp | iat + 8 小時 |

---

## 各環境行為對照

| 環境 | 無 Bearer Token | 有 Bearer Token |
|---|---|---|
| Development | 自動以假用戶通過認證 | 走真實 JWT Bearer 驗證 |
| Production / Staging | 401 Unauthorized | 走真實 JWT Bearer 驗證 |

---

## 注意事項

- `POST /Auth/Logout` 需要 `Authorization: Bearer <token>` 才能執行撤銷。Development 環境若以 DevBypass（無 Token）呼叫 Logout，會回傳 `400 BadRequest`，因為沒有 jti 可撤銷。
- DevBypass 的 jti 每次都是新 GUID，無需也無法撤銷。
- 此機制僅在 `builder.Environment.IsDevelopment()` 為 true 時才會被 Program.cs 註冊，正式環境不會存在此 Scheme。

---

## Program.cs 相關片段

```csharp
// 僅 Development 環境才讀取與註冊 DevBypass
if (builder.Environment.IsDevelopment())
{
    var devUser = builder.Configuration
        .GetSection(DevBypassUserSettings.SectionName)
        .Get<DevBypassUserSettings>() ?? new DevBypassUserSettings();
    builder.Services.AddSingleton(devUser);
}

// Development 使用 DevBypass 作為預設 Scheme
AddAuthentication(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DefaultAuthenticateScheme = DevBypassAuthenticationHandler.SchemeName;
        options.DefaultChallengeScheme    = DevBypassAuthenticationHandler.SchemeName;
    }
    else
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    }
});

if (builder.Environment.IsDevelopment())
    authBuilder.AddScheme<DevBypassAuthenticationOptions, DevBypassAuthenticationHandler>(
        DevBypassAuthenticationHandler.SchemeName, _ => { });
```
