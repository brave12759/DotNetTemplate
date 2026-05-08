# Template.Common — 共用核心說明

[← 返回方案 README](../README.md)

本專案為 DDD **Shared Kernel**，定義所有跨層共用的介面、模型、設定類別與工具。**不依賴任何其他專案層**，被所有層引用。

---

## 架構定位

```
Template.WebApi
Template.BusinessRule      ← 所有層都依賴 Template.Common
DataAccess
        ↓ 引用
Template.Common（本專案 — Shared Kernel）
        不依賴任何其他層
```

---

## 目錄結構

```
Template.Common/
├── BackgroundQueue/
│   ├── BackgroundJob.cs                # 背景工作資料
│   ├── BackgroundJobDto.cs             # 背景工作查詢明細
│   ├── BackgroundJobQueryResult.cs     # 背景工作分頁查詢結果
│   ├── BackgroundJobSummaryDto.cs      # 背景工作統計資料
│   ├── BackgroundJobStatus.cs          # 背景工作狀態
│   ├── BackgroundWorkType.cs           # 背景工作類型 enum
│   ├── IBackgroundJobHandler.cs        # 背景工作處理器介面
│   ├── IBackgroundJobMonitorService.cs # 背景工作查詢服務介面
│   ├── IBackgroundTaskQueue.cs         # 背景資料庫佇列介面
│   └── Doc/
│       └── BackgroundQueue.md
├── Enums/
│   └── MessageEnum.cs                  # HTTP 狀態碼列舉（含 [Description] 中文說明）
├── Extensions/
│   └── EnumExtensions.cs               # GetDescription() / GetValue() 擴充方法
├── Models/
│   ├── CurrentUser.cs                  # JWT Claims 解析後的當前登入使用者物件
│   ├── LoginResult.cs                  # 登入操作結果（Success / Token / ErrorMessage）
│   ├── ResponseMessage.cs              # 統一 API 回傳格式 ResponseMessage<T>
│   ├── User/
│   │   ├── UserDto.cs                  # 使用者輸出模型（不含密碼）
│   │   ├── UserCreateRequest.cs        # 建立使用者請求
│   │   ├── UserUpdateRequest.cs        # 更新使用者請求
    │   ├── UserResetPasswordRequest.cs # 重設密碼請求
    │   └── UserChangePasswordRequest.cs # 修改密碼請求（需驗證舊密碼）
│   └── Doc/
│       ├── CurrentUser.md
│       └── ResponseMessage.md
├── Services/
│   ├── ICryptographyService.cs         # 加解密 / 雜湊服務介面    ├── IPasswordManager.cs             # 密碼管理服務介面（規則驗證 + 雜湊）│   ├── ICurrentUserService.cs          # 當前使用者服務介面
│   ├── IJwtService.cs                  # JWT Token 產生服務介面
│   ├── ILoginService.cs                # 登入服務介面
│   ├── IUserService.cs                 # 使用者管理服務介面
│   └── ITokenRevocationService.cs      # Token 撤銷服務介面
└── Settings/    ├── CorsSettings.cs                 # CORS 跨域存取設定    ├── ApiSettings.cs                  # Swagger 標題
    ├── CryptographyKeySettings.cs      # AES / RSA 金鑰
    ├── DatabaseSettings.cs             # DB 連線字串
    ├── HashSettings.cs                 # PBKDF2 迭代次數
    ├── HttpsSettings.cs                # HTTPS / HSTS / PFX 憑證
    ├── JwtSettings.cs                  # JWT 金鑰 / Issuer / 有效時間
    ├── LogSettings.cs                  # Serilog 檔案輸出
    ├── TimeZoneSettings.cs             # 應用程式時區（IANA）
    └── Doc/
        └── Settings.md
└── Utils/
    ├── ClockUtil.cs                    # 時間日期格式、解析與轉換工具
    └── Doc/
        └── ClockUtil.md
```

---

## 服務說明文件

| 類別 / 功能 | 說明 | 文件 |
|---|---|---|
| CurrentUser | JWT Claims 解析、各層取用方式、DevBypass 行為 | [CurrentUser.md](Models/Doc/CurrentUser.md) |
| ResponseMessage | 統一回傳格式、MessageEnum、SkipResponseWrap | [ResponseMessage.md](Models/Doc/ResponseMessage.md) |
| 設定類別 | 全部 Settings 欄位說明與 appsettings 範例 | [Settings.md](Settings/Doc/Settings.md) |
| ClockUtil | DateTime / DateTimeOffset / DateOnly / TimeOnly 格式與轉換 | [ClockUtil.md](Utils/Doc/ClockUtil.md) |
| BackgroundQueue | 背景資料庫佇列介面、工作資料、處理器介面與查詢 DTO | [BackgroundQueue.md](BackgroundQueue/Doc/BackgroundQueue.md) |

---

## 服務介面一覽

| 介面 | 實作專案 | 實作類別 |
|---|---|---|
| `ILoginService` | Template.BusinessRule | `LoginService` |
| `IUserService` | Template.BusinessRule | `UserService` |
| `ICryptographyService` | Template.BusinessRule | `CryptographyService` |
| `IPasswordManager` | Template.BusinessRule | `PasswordManager` |
| `ITokenRevocationService` | Template.BusinessRule | `EfCoreTokenRevocationService` / `InMemoryTokenRevocationService` |
| `IJwtService` | Template.WebApi | `JwtService` |
| `ICurrentUserService` | Template.WebApi | `CurrentUserService` |
