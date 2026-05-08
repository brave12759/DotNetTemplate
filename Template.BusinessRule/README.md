# Template.BusinessRule — 邏輯層說明

[← 返回主 README](../README.md)

.NET DDD Application/Domain Layer，放置各功能模組的商業邏輯、服務介面、服務實作與該模組專用模型。本層不直接被外部呼叫，僅透過 DI 容器由 `Template.WebApi` 注入。

本專案採模組化設計。各功能模組都需要由開發者依專案需求獨立註冊 DI；若專案不需要某個模組，可直接移除該模組資料夾，以及 `Template.WebApi/Controllers` 中對應的 Controller與對應的測試。

---

## 架構定位

```
Template.BusinessRule/      ← 本層：功能模組、服務介面、Service 實作 + BaseService
        ↑ 使用（透過 DI）
Template.WebApi/            ← DI 裝配 + Controllers 呼叫
```

---

## 目錄結構

```
Template.BusinessRule/
├── BaseService.cs                              # 邏輯層基底（提供 Db / LogDb / CurrentUserService）
├── BackgroundQueue/
│   └── Services/
│       ├── BackgroundJobMonitorService.cs       # 背景工作佇列查詢服務
│       ├── DbBackgroundTaskQueue.cs            # 資料庫背景工作佇列
│       └── QueuedBackgroundService.cs          # 背景佇列 HostedService
├── CryptographyService/
│   ├── Doc/
│   │   └── CryptographyService.md
│   ├── Models/
│   │   └── CryptographyServiceRequests.cs     # 請求模型（API Controller 使用）
│   └── Services/
│       └── CryptographyService.cs             # AES / RSA / PBKDF2 實作
├── LoginService/
│   ├── Doc/
│   │   └── LoginService.md
│   └── Services/
│       └── LoginService.cs                    # 帳密驗證 / JWT 發放 / 登出
├── UserService/
│   ├── Doc/
│   │   └── UserService.md
│   └── Services/
│       └── UserService.cs                     # 使用者 CRUD / 重設密碼
├── MenuTreeService/
│   ├── Doc/
│   │   └── MenuTreeService.md
│   ├── Models/
│   │   ├── MenuTreeCreateRequest.cs
│   │   ├── MenuTreeDto.cs
│   │   └── MenuTreeUpdateRequest.cs
│   └── Services/
│       ├── IMenuTreeService.cs
│       └── MenuTreeService.cs                 # 選單樹 CRUD / 階層樹查詢
├── PasswordManager/
│   ├── Doc/
│   │   └── PasswordManager.md
│   └── Services/
│       └── PasswordManager.cs                 # 密碼規則驗證 + 雜湊委派給 ICryptographyService
└── TokenRevocationService/
    ├── Doc/
    │   └── TokenRevocationService.md
    └── Services/
        ├── EfCoreTokenRevocationService.cs    # EF Core LINQ 實作（多節點）
        └── InMemoryTokenRevocationService.cs  # In-Memory 實作（單機/開發）
```

---

## BaseService

所有服務的基底類別，透過 `IServiceProvider` Lazy 載入常用相依性，避免建構子注入過多參數。

| 屬性 | 型別 | 說明 |
|---|---|---|
| `Db` | `ProjectDbContext` | 主業務資料庫（延遲載入） |
| `LogDb` | `LogDbContext` | 日誌資料庫（延遲載入） |
| `CurrentUserService` | `ICurrentUserService` | 當前登入使用者（延遲載入） |

**用法**：
```csharp
public class MyService(IServiceProvider sp) : BaseService(sp), IMyService
{
    public async Task DoSomethingAsync()
    {
        var users = await Db.Sys_UserInfos.AsNoTracking().ToListAsync();
        var currentUser = CurrentUserService.CurrentUser;
    }
}
```

---

## 服務一覽

| 服務 | 介面 | 實作 | 文件 |
|---|---|---|---|
| 登入 / 登出 | `ILoginService` | `LoginService` | [LoginService.md](LoginService/Doc/LoginService.md) |
| 使用者管理 | `IUserService` | `UserService` | [UserService.md](UserService/Doc/UserService.md) |
| 選單樹 | `IMenuTreeService` | `MenuTreeService` | [MenuTreeService.md](MenuTreeService/Doc/MenuTreeService.md) |
| 背景資料庫佇列 | `IBackgroundTaskQueue` | `DbBackgroundTaskQueue` | [BackgroundQueue.md](../Template.Common/BackgroundQueue/Doc/BackgroundQueue.md) |
| 背景佇列查詢 | `IBackgroundJobMonitorService` | `BackgroundJobMonitorService` | [BackgroundQueue.md](../Template.Common/BackgroundQueue/Doc/BackgroundQueue.md) |
| 加解密 / 雜湊 | `ICryptographyService` | `CryptographyService` | [CryptographyService.md](CryptographyService/Doc/CryptographyService.md) |
| 密碼管理 | `IPasswordManager` | `PasswordManager` | [PasswordManager.md](PasswordManager/Doc/PasswordManager.md) |
| Token 撤銷 | `ITokenRevocationService` | `EfCoreTokenRevocationService` / `InMemoryTokenRevocationService` | [TokenRevocationService.md](TokenRevocationService/Doc/TokenRevocationService.md) |

---

## 模組註冊與移除原則

每個功能模組都應視專案需求獨立註冊。不要假設只要引用 `Template.BusinessRule` 就會自動啟用所有功能。

- 需要某模組時：依該模組文件註冊 DI，並保留對應 Controller。
- 不需要某模組時：移除 `Template.BusinessRule/{ModuleName}` 資料夾、`Template.WebApi/Controllers` 中對應 Controller，以及 `Template.Test/Tests` 中對應測試。
- 若模組有資料表需求：依該模組文件建立資料表或移除相關資料表。
- 若模組介面放在模組資料夾內：Controller 應直接引用該模組命名空間。

例如不需要選單樹功能時，可移除：

```text
Template.BusinessRule/MenuTreeService/
Template.WebApi/Controllers/MenuTreeController.cs
Template.Test/Tests/MenuTreeServiceTests.cs
```

## DI 註冊範例（Program.cs）

```csharp
builder.Services.AddBusinessRuleServices(databaseSettings, backgroundQueueSettings);
```

業務邏輯層 DI 必須集中在 `Template.BusinessRule/Extensions/ServiceCollectionExtensions.cs`。不要在 `Program.cs` 分散註冊 BusinessRule service；若新增業務服務或背景工作基礎設施，請統一加到 `AddBusinessRuleServices`。

`IJwtService`、`ICurrentUserService` 屬於 WebApi 宿主層服務，仍由 `Program.cs` 註冊。

> `EfCoreTokenRevocationService` 依賴 `LogDbContext`（Scoped），因此本身也必須以 `Scoped` 註冊。`InMemoryTokenRevocationService` 無外部相依，可用 `Singleton`。
