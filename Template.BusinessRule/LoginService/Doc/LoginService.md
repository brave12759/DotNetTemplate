# LoginService 登入服務

[專案 README](../../../README.md) / [BusinessRule README](../../README.md)

## 功能說明

`LoginService` 負責登入驗證、登入失敗鎖定、密碼過期判斷、JWT Token 產生、Token 刷新與登出撤銷。

## 檔案位置

| 類型 | 路徑 |
|---|---|
| 介面 | `Template.BusinessRule/LoginService/Services/ILoginService.cs` |
| 實作 | `Template.BusinessRule/LoginService/Services/LoginService.cs` |
| 回傳模型 | `Template.Common/Models/LoginResult.cs` |
| API Controller | `Template.WebApi/Controllers/AuthController.cs` |
| 測試 | `Template.Test/Tests/LoginServiceTests.cs` |

## 系統設定

登入相關設定放在 `Sys_BasicSettings`，`Type = SystemSetting`。

| Key | 說明 | 單位 |
|---|---|---|
| `LoginFailLimit` | 密碼錯誤幾次後進入登入鎖定 | 次 |
| `AccountFailLock` | 登入失敗鎖定時間；若為 0 或未設定，達到錯誤次數後視為需人工處理 | 分鐘 |
| `PassWordExpire` | 密碼有效天數；若為 0 或未設定，不檢查密碼過期 | 天 |

設定不存在、格式錯誤或小於等於 0 時，服務會視為 0。

## 登入鎖定規則

系統不另外存 `LockoutEndTime`。帳號是否被鎖定由下列資料推算：

```text
LoginFailCount >= LoginFailLimit
且
UpdatedTime + AccountFailLock 分鐘 > 現在時間
```

鎖定規則如下：

- 平常正常登入或尚未達到錯誤上限時，不會有額外鎖定狀態。
- 密碼錯誤時會累加 `LoginFailCount`，並更新 `UpdatedTime` 作為鎖定起算點。
- 錯誤次數達到 `LoginFailLimit` 時，回傳帳號鎖定。
- 鎖定期間再次嘗試登入，會更新 `UpdatedTime`，讓鎖定時間從該次嘗試重新起算。
- 鎖定時間到期後，下一次登入會先清除 `LoginFailCount`，但不會自動啟用被人工停用的帳號。
- `AccountFailLock` 為 0 或未設定時，達到錯誤上限後不自動解鎖，需由管理員重設密碼或清除失敗次數。

## 登入流程

1. 檢查帳號與密碼是否有輸入。
2. 依 `UserId` 查詢使用者。
3. 讀取 `LoginFailLimit` 與 `AccountFailLock`。
4. 若帳號仍在鎖定期間，延長鎖定並回傳鎖定結果。
5. 若鎖定已到期，清除 `LoginFailCount`。
6. 檢查帳號是否啟用。
7. 驗證密碼；密碼錯誤時累加 `LoginFailCount`。
8. 密碼正確後，依 `Sys_UserPasswordHistory` 最後一筆 `ChangedTime` 與 `PassWordExpire` 判斷密碼是否過期。
9. 登入成功後清除 `LoginFailCount`、更新最後登入資訊，並產生 JWT Token。

## Token 刷新流程

1. 前端帶目前有效的 Bearer Token 呼叫 `POST /Auth/refresh`。
2. API 從 Token 取得 `UserId`、`jti` 與 `exp`。
3. 服務確認使用者存在、啟用、未被登入失敗鎖定，且密碼未過期。
4. 產生新的 JWT Token。
5. 將舊 Token 的 `jti` 加入撤銷清單。
6. 回傳新的 Token。

## API

| API | 權限 | 說明 |
|---|---|---|
| `POST /Auth/login` | 匿名 | 使用帳號密碼登入並取得 JWT Token |
| `POST /Auth/refresh` | 需 JWT | 使用目前尚未過期的 JWT Token 換取新 Token，並撤銷舊 Token |
| `GET /Auth/me` | 需 JWT | 取得目前 Token 內的使用者資訊 |
| `POST /Auth/logout` | 需 JWT | 登出並撤銷目前 Token |

## LoginResult

| 欄位 | 說明 |
|---|---|
| `Success` | 是否成功 |
| `Token` | 成功時回傳的 JWT Token |
| `ErrorMessage` | 失敗訊息 |
| `AccountDisabled` | 帳號是否因鎖定或停用而不可登入 |
| `PasswordExpired` | 密碼是否已過期 |
