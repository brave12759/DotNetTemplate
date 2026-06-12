# CurrentUser 說明文件

[← 返回方案 README](../../../README.md) ｜ [← 返回上層 README](../../README.md)

## 概述

`CurrentUser` 是由 JWT Claims 解析而來的當前登入使用者資訊物件，位於 `Template.Common` 層，可被所有專案層引用。透過 `ICurrentUserService` 注入取得，無需直接操作 `HttpContext`。

---

## 架構位置

```
Template.Common/
├── Models/
│   └── CurrentUser.cs                 # 使用者資訊模型
└── Services/
    └── ICurrentUserService.cs         # 介面定義

Template.WebApi/
└── Services/
    └── CurrentUserService.cs          # 從 IHttpContextAccessor 解析 JWT Claims

Template.BusinessRule/
└── BaseService.cs                     # 透過 Lazy<ICurrentUserService> 供邏輯層使用
```

---

## 模型定義

```csharp
public class CurrentUser
{
    public string UserId      { get; set; }  // 使用者帳號
    public string Email       { get; set; }  // 電子郵件
    public string MobilePhone { get; set; }  // 聯絡電話
    public string DeptId      { get; set; }  // 部門 ID
    public string Ip          { get; set; }  // 登入來源 IP
    public long   IssuedTime  { get; set; }  // JWT 簽發時間（Unix Timestamp）
    public long   ExpiredTime { get; set; }  // JWT 到期時間（Unix Timestamp）
    public string TokenId     { get; set; }  // JWT jti（Token 唯一識別碼）

    // 選用：啟用 RoleGroupService 後，可放入使用者角色群組。
    // public List<CurrentUserRoleGroup> RoleGroups { get; set; } = [];

    // 選用：啟用 FunctionPermissionService 後，可放入使用者功能操作權限。
    // public List<CurrentUserFunctionPermission> FunctionPermissions { get; set; } = [];
}
```

---

## 選用角色群組與功能操作權限

若專案需要在 `CurrentUser` 直接攜帶角色群組或功能操作權限，可取消註解 `CurrentUser.cs` 內預留的 List 欄位與模型：

| 欄位 | 型別 | 說明 |
|---|---|---|
| RoleGroups | `List<CurrentUserRoleGroup>` | 使用者擁有的角色群組清單，通常由 `RoleGroupService` 取得，並以 `RoleGroupId` 作為 Key 值 |
| FunctionPermissions | `List<CurrentUserFunctionPermission>` | 使用者經由角色群組取得的功能操作權限，通常由 `FunctionPermissionService` 取得 |

功能操作權限在實務上通常會高度綁定角色群組。若要放入 `CurrentUser`，建議由登入流程或授權流程先透過 `RoleGroupId` 彙整使用者權限，再寫入 `CurrentUser`；預設程式先保留註解，避免未啟用角色群組或功能權限模組的專案被迫增加相依。

---

## Claims 對照表

| 欄位 | JWT Claim 類型 | 說明 |
|---|---|---|
| UserId | `ClaimTypes.Name` | 使用者帳號 |
| Email | `ClaimTypes.Email` | 電子郵件 |
| MobilePhone | `ClaimTypes.MobilePhone` | 聯絡電話 |
| DeptId | `"dept_id"` | 部門 ID（自訂 claim） |
| Ip | `"ip"` | 登入來源 IP（自訂 claim） |
| IssuedTime | `JwtRegisteredClaimNames.Iat` | 簽發時間（long） |
| ExpiredTime | `JwtRegisteredClaimNames.Exp` | 到期時間（long） |
| TokenId | `JwtRegisteredClaimNames.Jti` | Token 唯一識別碼 |
| RoleGroups | `"role_groups"` | 選用；使用者角色群組 JSON，預設不啟用 |
| FunctionPermissions | `"function_permissions"` | 選用；使用者功能操作權限 JSON，預設不啟用 |

---

## 使用方式

### Controller 層

```csharp
public class MyController(
    ILogger<MyController> logger,
    ICurrentUserService currentUserService) : AuthenticationController(logger)
{
    public IActionResult Example()
    {
        var user = currentUserService.CurrentUser;
        // user.UserId, user.Email, user.DeptId ...
    }
}
```

### BusinessRule 層（透過 BaseService）

```csharp
public class MyService(IServiceProvider serviceProvider) : BaseService(serviceProvider)
{
    public void Example()
    {
        var user = CurrentUserService.CurrentUser;
        // user.UserId, user.DeptId ...
    }
}
```

---

## 未登入時的預設值

若請求未通過驗證（例如 `[AllowAnonymous]` 端點），`CurrentUserService` 會回傳空的 `CurrentUser`，所有字串欄位為 `string.Empty`，數值欄位為 `0`。

---

## Development 環境（DevBypass）

在 Development 環境且未提供 Bearer Token 時，由 `DevBypassAuthenticationHandler` 自動注入假用戶 Claims，`CurrentUser` 值對應 `appsettings.Development.json` 的 `DevBypassUser` 區段：

```json
"DevBypassUser": {
  "UserId": "dev",
  "Email": "dev@localhost",
  "MobilePhone": "",
  "DeptId": "0"
}
```

`Ip` 固定為 `127.0.0.1`，`TokenId` 為每次請求產生的新 GUID，`IssuedTime` / `ExpiredTime` 為當下時間與 +8 小時。

---

## DI 註冊

```csharp
// Program.cs
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
```

`CurrentUserService` 使用 `Lazy<CurrentUser>` 延遲解析，僅在第一次存取 `.CurrentUser` 時才讀取 Claims。
