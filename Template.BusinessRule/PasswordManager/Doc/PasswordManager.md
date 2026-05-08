# PasswordManager 說明文件

[← 返回方案 README](../../../README.md) ｜ [← 返回 Template.BusinessRule](../../README.md)

## 概述

`PasswordManager` 封裝密碼管理的兩大責任：

1. **規則驗證**：強制執行密碼複雜度規則（長度、字母與數字混合），屬於領域業務規則。
2. **雜湊委派**：將密碼的雜湊與驗證工作委派給 `ICryptographyService`（PBKDF2），屬於基礎設施。

---

## DDD 層次定位

| 類型 | 位置 | 原因 |
|---|---|---|
| 介面 `IPasswordManager` | `Template.Common/Services/` | Shared Kernel — 跨層共用的契約 |
| 實作 `PasswordManager` | `Template.BusinessRule/PasswordManager/Services/` | 密碼複雜度規則 = 領域業務規則，屬 Domain/Application 層 |

> **為何實作不放在 Common？**
> `Common` 作為 Shared Kernel 應僅存放契約（介面、DTO、Enum），不應包含業務邏輯。密碼複雜度規則（長度限制、字母+數字強制）是應用程式的密碼政策，屬業務規則而非基礎設施。

---

## 介面與實作位置

| 類型 | 位置 |
|---|---|
| 介面 | `Template.Common/Services/IPasswordManager.cs` |
| 實作 | `Template.BusinessRule/PasswordManager/Services/PasswordManager.cs` |

---

## 密碼規則

| 規則 | 說明 |
|---|---|
| 不可為空 | `null`、空字串、純空白均不允許 |
| 最短長度 | 至少 **8** 個字元 |
| 字元種類 | 必須同時包含**英文字母**與**數字** |

違反規則時丟出 `ArgumentException`，訊息說明具體違規項目。

---

## 方法一覽

| 方法 | 簽章 | 說明 |
|---|---|---|
| `ValidateNewPassword` | `void ValidateNewPassword(string password)` | 驗證密碼是否符合複雜度規則，違規丟出例外 |
| `HashForStorage` | `string HashForStorage(string password)` | 先驗證規則，再透過 `ICryptographyService.Hash()` 產生 PBKDF2 雜湊 |
| `Verify` | `bool Verify(string password, string storedHash)` | 比對明文與儲存雜湊是否一致（登入驗證、修改密碼舊密碼確認使用） |

---

## 使用場景

| 場景 | 呼叫方 | 使用的方法 |
|---|---|---|
| 使用者登入 | `LoginService` | `Verify(plainText, storedHash)` |
| 建立使用者 | `UserService.CreateAsync` | `HashForStorage(password)` |
| 重設密碼 | `UserService.ResetPasswordAsync` | `HashForStorage(newPassword)` |
| 修改密碼 | `UserService.ChangePasswordAsync` | `Verify(oldPassword, storedHash)` → `HashForStorage(newPassword)` |

---

## DI 注冊

```csharp
// Program.cs
builder.Services.AddScoped<IPasswordManager, PasswordManager>();
```

---

## 測試覆蓋

測試位於 `Template.Test/Tests/PasswordManagerTests.cs`，使用**真實** `CryptographyService`（含 RSA 金鑰）：

| 測試方法 | 驗證行為 |
|---|---|
| `HashForStorage_ValidPassword_ShouldReturnHash` | 合法密碼可產生雜湊 |
| `Verify_ValidPair_ShouldReturnTrue` | 正確明文與雜湊比對回傳 `true` |
| `Verify_InvalidPair_ShouldReturnFalse` | 錯誤明文與雜湊比對回傳 `false` |
| `ValidateNewPassword_TooShort_ShouldThrow` | 長度不足丟出例外 |
| `ValidateNewPassword_NoDigit_ShouldThrow` | 無數字丟出例外 |
| `ValidateNewPassword_NoLetter_ShouldThrow` | 無字母丟出例外 |
