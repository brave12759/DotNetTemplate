# Template.Common 共用專案

[根目錄 README](../README.md)

`Template.Common` 是跨層共用的 shared kernel，只放通用模型、共用介面、設定類別與工具。業務流程介面與實作放在 `Template.BusinessRule`；Web/API 基礎設施實作放在 `Template.WebApi`。

## 專案結構

```text
Template.Common/
  BackgroundQueue/
    BackgroundJob.cs
    BackgroundJobDto.cs
    BackgroundJobQueryResult.cs
    BackgroundJobStatus.cs
    BackgroundJobSummaryDto.cs
    BackgroundWorkType.cs
    IBackgroundJobHandler.cs
    IBackgroundJobMonitorService.cs
    IBackgroundTaskQueue.cs
    Doc/BackgroundQueue.md
  Enums/
    MessageEnum.cs
  Extensions/
    EnumExtensions.cs
  Models/
    CurrentUser.cs
    LoginResult.cs
    ResponseMessage.cs
    Jwt/
      JwtSettingDto.cs
      JwtSettingUpdateRequest.cs
    User/
      UserDto.cs
      UserCreateRequest.cs
      UserUpdateRequest.cs
      UserResetPasswordRequest.cs
      UserChangePasswordRequest.cs
    Doc/
      CurrentUser.md
      ResponseMessage.md
  Services/
    ICurrentUserService.cs
    IJwtService.cs
  Settings/
    ApiSettings.cs
    BackgroundQueueSettings.cs
    CorsSettings.cs
    CryptographyKeySettings.cs
    DatabaseSettings.cs
    HashSettings.cs
    LogSettings.cs
    TimeZoneSettings.cs
    Doc/Settings.md
  SignalR/
    ISignalRQueueService.cs
    SignalRClientMethods.cs
    SignalRQueuedMessage.cs
    SignalRTargetType.cs
    Doc/SignalR.md
  FileStorage/
    FileStorageContracts.cs
    Doc/FileStorage.md
  Utils/
    ClockUtil.cs
    Doc/ClockUtil.md
```

## 介面放置規則

| 介面 | 放置位置 | 實作 |
|---|---|---|
| `IUserService` | `Template.BusinessRule/UserService` | `UserService` |
| `ILoginService` | `Template.BusinessRule/LoginService` | `LoginService` |
| `ICryptographyService` | `Template.BusinessRule/CryptographyService` | `CryptographyService` |
| `IPasswordManager` | `Template.BusinessRule/PasswordManager` | `PasswordManager` |
| `ITokenRevocationService` | `Template.BusinessRule/TokenRevocationService` | EF Core 或 in-memory 實作 |
| `ISsoService` | `Template.BusinessRule/SsoService` | `SsoService` |
| `ISignalRQueueService` | `Template.Common/SignalR` | `SignalRQueueService` |
| `IJwtService` | `Template.Common/Services` | `Template.WebApi/Services/JwtService` |
| `ICurrentUserService` | `Template.Common/Services` | `Template.WebApi/Services/CurrentUserService` |

## 設定說明

仍由設定檔或環境變數讀取的設定請參考 [Settings.md](Settings/Doc/Settings.md)。

JWT 設定不再存放於 `appsettings.json`，而是存放在 `Sys_BasicSettings`，`Type = JwtSetting`，並透過 JWT 設定 API 管理。

HTTPS 強制導向、HSTS 與憑證載入不由應用程式管理。HTTPS 由 IIS、反向代理、Load Balancer、Ingress 或 hosting platform 負責。

## 相關文件

| 範圍 | 文件 |
|---|---|
| 目前使用者模型 | [CurrentUser.md](Models/Doc/CurrentUser.md) |
| API 回應包裝 | [ResponseMessage.md](Models/Doc/ResponseMessage.md) |
| 設定 | [Settings.md](Settings/Doc/Settings.md) |
| 背景工作佇列 | [BackgroundQueue.md](BackgroundQueue/Doc/BackgroundQueue.md) |
| SignalR Queue | [SignalR.md](SignalR/Doc/SignalR.md) |
| FileStorage（含產表 SQL 與欄位描述語法） | [FileStorage.md](FileStorage/Doc/FileStorage.md) |
| 時間工具 | [ClockUtil.md](Utils/Doc/ClockUtil.md) |
