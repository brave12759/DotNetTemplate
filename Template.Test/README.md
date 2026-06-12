# Template.Test — 單元測試說明

[← 返回方案 README](../README.md)

本專案使用 **MSTest** 框架，負責對各層邏輯進行單元測試。

測試策略文件：
[TestStrategy.md](Doc/TestStrategy.md)

---

## 目錄結構

```
Template.Test/
├── Doc/
│   └── TestStrategy.md
├── Tests/
│   ├── CryptographyServiceTests.cs
│   ├── ClockUtilTests.cs
│   ├── BackgroundTaskQueueTests.cs
│   ├── InMemoryTokenRevocationServiceTests.cs
│   ├── MenuTreeServiceTests.cs
│   ├── RoleGroupServiceTests.cs
│   ├── FunctionPermissionServiceTests.cs
│   ├── ModelTests.cs
│   ├── EnumExtensionsTests.cs    
│   ├── PasswordManagerTests.cs
│   └── UserServiceTests.cs
└── Properties/
    └── AssemblyInfo.cs
```

---

## 目前核心測試涵蓋範圍

| 測試對象 | 覆蓋內容 |
|---|---|
| `UserService` | CRUD、篩選查詢、重設 / 修改密碼、重複帳號防呆、參數驗證 |
| `MenuTreeService` | CRUD、階層樹查詢、重複代碼防呆、父層循環防呆、刪除子節點保護 |
| `RoleGroupService` | CRUD、階層樹查詢、父層循環防呆、刪除子孫節點、使用者角色群組對應 |
| `FunctionPermissionService` | CRUD、階層樹查詢、一鍵補足 CRUDAF 操作權限、角色群組指派、使用者權限樹 |
| `PasswordManager` | 密碼雜湊、比對驗證、規則驗證（長度、字母、數字） |
| `CryptographyService` | AES 加解密對稱性、PBKDF2 雜湊驗證、錯誤金鑰格式 |
| `ClockUtil` | 格式化、解析、UTC/本地轉換、DateOnly/TimeOnly 互轉、Unix Time |
| `BackgroundQueue` | 資料庫工作入列、依 WorkType 取得、完成、失敗重排、統計摘要與明細查詢 |
| `InMemoryTokenRevocationService` | Revoke、IsRevoked、過期自動清理 |
| `ResponseMessage<T>` | Success / Fail 工廠方法、MessageEnum 對應 |
| `EnumExtensions` | GetDescription()、GetValue() |

---

## 核心邏輯優先原則

- 只測試核心商業邏輯與規則，避免對框架細節過度單元測試。
- 高成本、低效益項目（如 Middleware UI 細節、外部 I/O 行為）不列入此專案單元測試。
- 需要跨層驗證的情境改以整合測試或手動驗證補齊。
