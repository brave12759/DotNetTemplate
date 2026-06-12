# Token 撤銷服務說明文件

[← 返回方案 README](../../../README.md) ｜ [← 返回上層 README](../../README.md)

## 概述

Token 撤銷服務負責管理 JWT 登出狀態，讓已簽發但未過期的 Token 在登出後立即失效。本服務位於邏輯層（`Template.BusinessRule`），透過 EF Core LINQ 操作資料庫，或在無資料庫設定時降級為 In-Memory 模式。

---

## 架構位置

```
Template.DataAccess/
└── Models/
    └── TokenRevocation.cs             # 撤銷紀錄實體（dbo.TokenRevocation）

Template.DataAccess/
└── LogDbContext.cs                    # DbSet<TokenRevocation>，含 OnModelCreating 設定

Template.Common/
└── Services/
    └── ITokenRevocationService.cs     # 介面定義

Template.BusinessRule/
└── TokenRevocationService/
    └── Services/
        ├── EfCoreTokenRevocationService.cs   # EF Core LINQ 實作（多節點/正式環境）
        └── InMemoryTokenRevocationService.cs  # In-Memory 實作（單機/開發環境）
```

---

## 介面

```csharp
public interface ITokenRevocationService
{
    // 將 jti 加入撤銷清單，直到 exp 為止
    void Revoke(string tokenId, long expiredUnixTimeSeconds);

    // 檢查 jti 是否在撤銷清單中且尚未過期
    bool IsRevoked(string tokenId);
}
```

---

## 資料表結構（dbo.TokenRevocation）

| 欄位 | 型別 | 說明 |
|---|---|---|
| TokenId | nvarchar(128) PK | JWT jti（唯一識別碼） |
| ExpiresUtc | datetime2 | Token 到期時間（UTC） |
| RevokedTime | datetime2 | 撤銷操作時間（UTC） |

索引：`IX_TokenRevocation_ExpiresUtc`（支援清理過期紀錄的範圍查詢）。

---

## 兩種實作對照

| 項目 | EfCoreTokenRevocationService | InMemoryTokenRevocationService |
|---|---|---|
| 儲存位置 | SQL Server Log 資料庫 | 應用程式記憶體 |
| 多節點支援 | ✅ 共用同一 DB | ❌ 各節點獨立 |
| DI 生命週期 | Scoped | Singleton |
| 查詢方式 | EF Core LINQ | ConcurrentDictionary |
| 重啟後保留 | ✅ | ❌ |
| 前提條件 | LogConnectionString 非空 | 無 |

---

## EfCoreTokenRevocationService 運作流程

### Revoke（登出時）

```
LoginService.LogoutAsync(tokenId, expiredUnixTimeSeconds)
  → EfCoreTokenRevocationService.Revoke
    → 轉換 exp → DateTime UTC
    → logDb.TokenRevocations.Find(tokenId)
      ├─ 不存在 → Add 新紀錄
      └─ 已存在 → 更新 ExpiresUtc 與 RevokedTime
    → SaveChanges()
    → 清理所有 ExpiresUtc <= UtcNow 的過期紀錄（LINQ Where → RemoveRange）
    → SaveChanges()
```

### IsRevoked（每次 JWT 驗證時）

```
JwtBearerEvents.OnTokenValidated
  → EfCoreTokenRevocationService.IsRevoked(jti)
    → logDb.TokenRevocations.FirstOrDefault(t => t.TokenId == jti)
      ├─ 無紀錄 → false（未撤銷）
      ├─ 已過期 → Remove → SaveChanges() → false（自動清理）
      └─ 有效撤銷 → true → context.Fail("Token 已登出或撤銷。") → 401
```

---

## DI 自動選擇邏輯（Program.cs）

```csharp
if (!string.IsNullOrWhiteSpace(databaseSettings.LogConnectionString))
    builder.Services.AddScoped<ITokenRevocationService, EfCoreTokenRevocationService>();
else
{
    Log.Warning("DatabaseSettings.LogConnectionString 未設定，Token 撤銷將使用 In-Memory...");
    builder.Services.AddSingleton<ITokenRevocationService, InMemoryTokenRevocationService>();
}
```

> `EfCoreTokenRevocationService` 使用 `LogDbContext`（Scoped），因此自身也必須以 `Scoped` 註冊。

---

## 資料庫資料表初始化

`EfCoreTokenRevocationService` 首次被呼叫時，會透過 `LogDbContext.Database.EnsureCreated()` 確認資料表已建立（使用靜態 flag 確保只執行一次）。若日後改用 EF Core Migrations，可移除此機制改用 `dotnet ef database update`。

---

## 多節點部署注意事項

多台 API 伺服器共用同一 Log 資料庫時，登出操作對所有節點立即生效（下次 JWT 驗證時皆會查詢 DB）。若需要進一步降低 DB 壓力，可在 `IsRevoked` 前加入短時間記憶體快取（如 `IMemoryCache`，TTL ≤ Token 有效期）。
