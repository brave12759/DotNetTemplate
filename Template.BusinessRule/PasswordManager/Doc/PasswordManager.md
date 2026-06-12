# PasswordManager 密碼管理服務

[專案 README](../../../README.md) / [BusinessRule README](../../README.md)

## 功能範圍

`PasswordManager` 負責密碼強度驗證、密碼雜湊與密碼驗證。

## 密碼規則

新密碼必須符合下列條件：

- 長度至少 12 碼。
- 至少包含一個大寫英文字母。
- 至少包含一個小寫英文字母。
- 至少包含一個數字。
- 至少包含一個符號。

範例：`ValidPass@123`

不符合規則時會丟出 `ArgumentException`。

## 關聯檔案

| 類型 | 路徑 |
|---|---|
| 介面 | `Template.BusinessRule/PasswordManager/Services/IPasswordManager.cs` |
| 實作 | `Template.BusinessRule/PasswordManager/Services/PasswordManager.cs` |
| 測試 | `Template.Test/Tests/PasswordManagerTests.cs` |

## 主要方法

| 方法 | 說明 |
|---|---|
| `ValidateNewPassword(string password)` | 驗證新密碼是否符合密碼規則 |
| `HashForStorage(string password)` | 驗證密碼後透過 `ICryptographyService.Hash` 產生儲存用雜湊 |
| `Verify(string password, string storedHash)` | 驗證明文密碼是否符合已儲存雜湊 |

