# UserService 使用者服務

[專案 README](../../../README.md) / [BusinessRule README](../../README.md)

## 功能說明

`UserService` 負責系統使用者資料管理，包含使用者 CRUD、部門關聯、密碼重設與密碼變更。

- 使用者清單查詢
- 使用者單筆查詢
- 建立使用者
- 更新使用者基本資料與所屬部門
- 刪除使用者
- 重設密碼
- 使用者變更密碼

使用者資料透過 `DeptId` 對應 `Sys_Department.DeptId`。建立與更新使用者時，服務會檢查部門是否存在；清單輸出會回傳 `DeptName` 方便前端顯示。

## 密碼歷程

使用者主檔 `Sys_UserInfo` 只保存目前密碼雜湊值；每次建立、重設或變更密碼時，服務會同步寫入 `Sys_UserPasswordHistory`。

密碼歷程用途：

- 記錄密碼異動時間與異動人員。
- 透過 `ChangeType` 區分初始建立、管理員重設、使用者自行變更。
- 登入時用最後一筆 `ChangedTime` 判斷密碼是否過期。
- 日後若要限制不可重複使用最近 N 次密碼，可直接查詢歷程表。

同時，建立使用者、重設密碼、變更密碼也會透過 `LogService` 寫入稽核日誌。稽核日誌只記錄異動種類與目標使用者，不寫入密碼明文或密碼雜湊。

## 檔案位置

| 類型 | 路徑 |
|---|---|
| 介面 | `Template.BusinessRule/UserService/Services/IUserService.cs` |
| Request/DTO | `Template.Common/Models/User` |
| 服務實作 | `Template.BusinessRule/UserService/Services/UserService.cs` |
| API Controller | `Template.WebApi/Controllers/UserController.cs` |
| 測試 | `Template.Test/Tests/UserServiceTests.cs` |
| 使用者 Entity | `Template.DataAccess/ProjectDbContext/Sys_UserInfo.cs` |
| 密碼歷程 Entity | `Template.DataAccess/ProjectDbContext/Sys_UserPasswordHistory.cs` |
| 部門 Entity | `Template.DataAccess/ProjectDbContext/Sys_Department.cs` |

## 服務方法

| 方法 | 說明 |
|---|---|
| `GetListAsync(keyword, isEnable, deptId, includeSubDepartments)` | 查詢使用者清單，支援關鍵字、啟用狀態、部門與子部門篩選 |
| `GetByIdAsync(int id)` | 依 Id 查詢單筆使用者 |
| `CreateAsync(UserCreateRequest request)` | 建立使用者、雜湊密碼並寫入密碼歷程 |
| `UpdateAsync(UserUpdateRequest request)` | 更新姓名、部門、電話、Email 與啟用狀態 |
| `DeleteAsync(int id)` | 刪除使用者 |
| `ResetPasswordAsync(UserResetPasswordRequest request)` | 管理員重設密碼，清除登入失敗次數並寫入密碼歷程 |
| `ChangePasswordAsync(UserChangePasswordRequest request)` | 驗證舊密碼後變更密碼並寫入密碼歷程 |

> 上述建立、更新、刪除、重設密碼、變更密碼流程會同步寫入 `UserOperationLog`，方便日後追查管理員操作與使用者密碼異動。

## 部門規則

- `UserCreateRequest.DeptId` 與 `UserUpdateRequest.DeptId` 必須大於 0。
- 建立或更新使用者時，`DeptId` 必須存在於 `Sys_Department`。
- `UserDto` 會回傳 `DeptId` 與 `DeptName`。
- `GetListAsync` 可用 `deptId` 查詢單一部門使用者。
- `includeSubDepartments=true` 時，會包含指定部門底下所有子部門的使用者。

## API

| API | 說明 |
|---|---|
| `GET /User?keyword=&isEnable=&deptId=&includeSubDepartments=` | 查詢使用者清單 |
| `GET /User/1` | 查詢單筆使用者 |
| `POST /User` | 建立使用者 |
| `PUT /User` | 完整更新使用者 |
| `PATCH /User/1` | 局部更新使用者 |
| `DELETE /User/1` | 刪除使用者 |
| `POST /User/reset-password` | 重設密碼 |
| `POST /User/change-password` | 變更密碼 |

`PATCH /User/1` 使用 JSON Patch 格式，只送需要調整的欄位。後端會先讀取現有使用者，套用 patch 後再走 `UpdateAsync`，因此部門檢查與稽核日誌規則與 `PUT /User` 相同。

```json
[
  { "op": "replace", "path": "/email", "value": "alice@example.com" },
  { "op": "replace", "path": "/isEnable", "value": true }
]
```

## Request 範例

```json
{
  "UserId": "alice",
  "UserName": "Alice",
  "Password": "ValidPass@123",
  "DeptId": 1,
  "MobilePhone": "0911222333",
  "Email": "alice@example.com",
  "IsEnable": true
}
```

## Response 範例

```json
{
  "Id": 1,
  "UserId": "alice",
  "UserName": "Alice",
  "DeptId": 1,
  "DeptName": "資訊部",
  "MobilePhone": "0911222333",
  "Email": "alice@example.com",
  "LoginFailCount": 0,
  "IsEnable": true,
  "LastLoginTime": null,
  "LastLoginIp": "",
  "CreatedTime": "2026-05-15T07:00:00Z",
  "UpdatedTime": "2026-05-15T07:00:00Z"
}
```

## 既有資料庫啟用密碼歷程

既有資料庫若已經有 `Sys_UserInfo.Password`，請先建立 `Sys_UserPasswordHistory`，再把目前密碼補成第一筆歷程。若資料庫先前曾加過 `PasswordUpdatedTime`，這段 SQL 會用它作為 `ChangedTime` 後移除該欄位；若沒有該欄位，會用 `UpdatedTime` 或 `CreatedTime` 補歷程時間。

```sql
IF OBJECT_ID(N'dbo.Sys_UserPasswordHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_UserPasswordHistory
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        UserId NVARCHAR(50) NOT NULL,
        PasswordHash NVARCHAR(500) NOT NULL,
        ChangeType INT NOT NULL CONSTRAINT DF_Sys_UserPasswordHistory_ChangeType DEFAULT (1),
        ChangedTime DATETIME2(7) NOT NULL CONSTRAINT DF_Sys_UserPasswordHistory_ChangedTime DEFAULT (SYSUTCDATETIME()),
        ChangedId NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_Sys_UserPasswordHistory PRIMARY KEY CLUSTERED (Id)
    );

    CREATE INDEX IX_Sys_UserPasswordHistory_UserId_ChangedTime
        ON dbo.Sys_UserPasswordHistory (UserId, ChangedTime DESC);
END;

IF COL_LENGTH(N'dbo.Sys_UserPasswordHistory', N'ChangeType') IS NULL
BEGIN
    ALTER TABLE dbo.Sys_UserPasswordHistory
        ADD ChangeType INT NOT NULL
            CONSTRAINT DF_Sys_UserPasswordHistory_ChangeType DEFAULT (1);
END;

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_Sys_UserPasswordHistory_UserId'
      AND parent_object_id = OBJECT_ID(N'dbo.Sys_UserPasswordHistory')
)
BEGIN
    ALTER TABLE dbo.Sys_UserPasswordHistory
        DROP CONSTRAINT FK_Sys_UserPasswordHistory_UserId;
END;

IF COL_LENGTH(N'dbo.Sys_UserInfo', N'PasswordUpdatedTime') IS NOT NULL
BEGIN
    EXEC(N'
        INSERT INTO dbo.Sys_UserPasswordHistory (UserId, PasswordHash, ChangeType, ChangedTime, ChangedId)
        SELECT u.UserId,
               u.Password,
               1,
               CASE
                   WHEN u.PasswordUpdatedTime IS NULL OR u.PasswordUpdatedTime = CONVERT(DATETIME2(7), ''0001-01-01'')
                       THEN ISNULL(NULLIF(u.UpdatedTime, CONVERT(DATETIME2(7), ''0001-01-01'')), u.CreatedTime)
                   ELSE u.PasswordUpdatedTime
               END,
               ISNULL(NULLIF(u.UpdatedId, ''''), ISNULL(NULLIF(u.CreatedId, ''''), ''system''))
        FROM dbo.Sys_UserInfo u
        WHERE NOT EXISTS (
            SELECT 1
            FROM dbo.Sys_UserPasswordHistory h
            WHERE h.UserId = u.UserId
        );
    ');

    DECLARE @passwordUpdatedTimeDefaultName SYSNAME;

    SELECT @passwordUpdatedTimeDefaultName = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c
        ON c.default_object_id = dc.object_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Sys_UserInfo')
      AND c.name = N'PasswordUpdatedTime';

    IF @passwordUpdatedTimeDefaultName IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE dbo.Sys_UserInfo DROP CONSTRAINT ' + QUOTENAME(@passwordUpdatedTimeDefaultName));
    END;

    ALTER TABLE dbo.Sys_UserInfo
        DROP COLUMN PasswordUpdatedTime;
END
ELSE
BEGIN
    INSERT INTO dbo.Sys_UserPasswordHistory (UserId, PasswordHash, ChangeType, ChangedTime, ChangedId)
    SELECT u.UserId,
           u.Password,
           1,
           ISNULL(NULLIF(u.UpdatedTime, CONVERT(DATETIME2(7), '0001-01-01')), u.CreatedTime),
           ISNULL(NULLIF(u.UpdatedId, ''), ISNULL(NULLIF(u.CreatedId, ''), 'system'))
    FROM dbo.Sys_UserInfo u
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Sys_UserPasswordHistory h
        WHERE h.UserId = u.UserId
    );
END;

IF COL_LENGTH(N'dbo.Sys_UserInfo', N'LockoutEndTime') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Sys_UserInfo
        DROP COLUMN LockoutEndTime;
END;
```
