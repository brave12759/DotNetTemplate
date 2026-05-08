# UserService 說明文件

[← 返回方案 README](../../../README.md) ｜ [← 返回 Template.BusinessRule](../../README.md)

## 概述

`UserService` 提供系統使用者的基本管理能力，涵蓋：

- 查詢清單（關鍵字、啟用狀態篩選）
- 查詢單筆
- 新增使用者
- 更新使用者基本資料
- 刪除使用者
- 重設密碼
- 修改密碼

密碼傲存與驗證均透過 `IPasswordManager`（PBKDF2 雜湊）處理，並強制密碼規則驗證。密碼變更分為兩種情境：

- **ResetPassword**：管理員強制變更，不需舊密碼。
- **ChangePassword**：使用者自行變更，需驗證舊密碼（`Verify`）再對新密碼執行規則驗證。

---

## 介面與實作位置

| 類型 | 位置 |
|---|---|
| 介面 | `Template.Common/Services/IUserService.cs` |
| 實作 | `Template.BusinessRule/UserService/Services/UserService.cs` |
| API 入口 | `Template.WebApi/Controllers/UserController.cs` |

---

## 方法一覽

| 方法 | 說明 |
|---|---|
| `GetListAsync(string? keyword, bool? isEnable)` | 依關鍵字（UserId/Email/MobilePhone/DeptId）與啟用狀態篩選 |
| `GetByIdAsync(int id)` | 依主鍵取得單筆使用者 |
| `CreateAsync(UserCreateRequest request)` | 建立使用者，密碼會先雜湊 |
| `UpdateAsync(UserUpdateRequest request)` | 更新基本資料（不含密碼） |
| `DeleteAsync(int id)` | 刪除使用者 |
| `ResetPasswordAsync(UserResetPasswordRequest request)` | 重設密碼（管理員強制變更，不需舊密碼） |
| `ChangePasswordAsync(UserChangePasswordRequest request)` | 修改密碼（需驗證舊密碼 + 新密碼規則驗證） |

---

## 重要規則

- `CreateAsync`：`UserId` 不可重複。
- `CreateAsync` / `ResetPasswordAsync`：密碼不可空白。
- `ChangePasswordAsync`：舊密碼驗證失敗時丟出 `UnauthorizedAccessException`；新密碼不符規則丟出 `ArgumentException`。
- `UpdateAsync` / `DeleteAsync` / `ResetPasswordAsync` / `ChangePasswordAsync`：若查無資料回傳 `false`。
- 所有 `id <= 0` 請求會丟出 `ArgumentException`。

---

## API 對應

| API | 說明 |
|---|---|
| `GET /User/List` | 取得清單 |
| `GET /User/GetById?id=1` | 取得單筆 |
| `POST /User/Create` | 新增使用者 |
| `PUT /User/Update` | 更新資料 |
| `DELETE /User/Delete?id=1` | 刪除使用者 |
| `POST /User/ResetPassword` | 重設密碼 |
| `POST /User/ChangePassword` | 修改密碼（需舊密碼） |
