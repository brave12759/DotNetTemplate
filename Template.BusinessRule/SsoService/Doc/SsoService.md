# SsoService

SSO client 資料存放在 `Sso_Client`。Client 認證資訊不放在 `Sys_BasicSettings`，避免把外部系統帳密資料混在一般系統設定中。

## 流程

1. 系統管理員建立 SSO client，並將 `ClientId` 與明文 `ClientSecret` 提供給外部系統。
2. 外部系統呼叫 `POST /Sso/Login`，用 client 認證資訊換取 Server Token。
3. API 驗證 client 後回傳短效 Server Token。
4. 其他系統可呼叫 `POST /Sso/ValidateToken`，確認 Token 是否由本系統發出、尚未過期、未被撤銷、`token_type = server`，且所屬 SSO client 仍為啟用狀態。

## API

| API | 權限 | 說明 |
|---|---|---|
| `GET /Sso/Clients` | `System.SsoClient:Manage` | 查詢 SSO client |
| `POST /Sso/CreateClient` | `System.SsoClient:Manage` | 建立 SSO client |
| `PUT /Sso/UpdateClient` | `System.SsoClient:Manage` | 更新 SSO client 或輪替 secret |
| `DELETE /Sso/DeleteClient?id=1` | `System.SsoClient:Manage` | 刪除 SSO client |
| `POST /Sso/Login` | 匿名 | 用 client 認證資訊換取 Server Token |
| `POST /Sso/ValidateToken` | 匿名 | 驗證 Server Token |

## 訊息代碼

SSO API 不直接散落硬寫字串，錯誤與一般處理結果統一由 `SsoMessageEnum` 管理。Controller 回傳格式如下，前端或外部系統應優先用 `Code` 或 `Name` 判斷流程，`Message` 只作為顯示或紀錄用途。

```json
{
  "code": 7,
  "name": "InvalidClientCredentials",
  "message": "ClientId 或 ClientSecret 錯誤。"
}
```

目前代碼分類：

| Code | Name | 說明 |
|---:|---|---|
| 1 | `IdMustBeGreaterThanZero` | Id 必須大於 0 |
| 2 | `ClientIdRequired` | ClientId 不可為空 |
| 3 | `ClientNameRequired` | ClientName 不可為空 |
| 4 | `ClientSecretRequired` | ClientSecret 不可為空 |
| 5 | `ClientIdAlreadyExists` | ClientId 已存在 |
| 6 | `ClientNotFound` | SSO client 不存在 |
| 7 | `InvalidClientCredentials` | ClientId 或 ClientSecret 錯誤 |
| 8 | `UpdatedSuccessfully` | 更新成功 |
| 9 | `DeletedSuccessfully` | 刪除成功 |

## 既有資料庫啟用 SSO

既有資料庫啟用 SSO 時，請依序執行下列 SQL。這段包含兩件事：

1. 建立 `Sso_Client` 資料表。
2. 建立管理 JWT 設定與 SSO client 所需的功能權限。

```sql
IF OBJECT_ID(N'dbo.Sso_Client', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sso_Client
    (
        Id INT IDENTITY(1,1) NOT NULL,
        ClientId NVARCHAR(100) NOT NULL,
        ClientName NVARCHAR(200) NOT NULL,
        ClientSecretHash NVARCHAR(500) NOT NULL,
        IsEnable BIT NOT NULL CONSTRAINT DF_Sso_Client_IsEnable DEFAULT (1),
        CreatedTime DATETIME2(7) NOT NULL CONSTRAINT DF_Sso_Client_CreatedTime DEFAULT (SYSUTCDATETIME()),
        CreatedId NVARCHAR(50) NOT NULL,
        UpdatedTime DATETIME2(7) NOT NULL CONSTRAINT DF_Sso_Client_UpdatedTime DEFAULT (SYSUTCDATETIME()),
        UpdatedId NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_Sso_Client PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Sso_Client_ClientId UNIQUE (ClientId)
    );

    CREATE INDEX IX_Sso_Client_IsEnable
        ON dbo.Sso_Client (IsEnable);
END;

DECLARE @now DATETIME2(7) = SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.JwtSetting')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (NULL, N'System.JwtSetting', N'System.JwtSetting', N'JWT 設定', NULL, N'', 900, 1, @now, N'system', @now, N'system');
END;

DECLARE @jwtSettingParentId INT =
    (SELECT FunctionPermissionId FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.JwtSetting');

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.JwtSetting:Manage')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (@jwtSettingParentId, N'System.JwtSetting:Manage', N'System.JwtSetting', N'JWT 設定', N'Manage', N'管理', 1, 1, @now, N'system', @now, N'system');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.SsoClient')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (NULL, N'System.SsoClient', N'System.SsoClient', N'SSO client 管理', NULL, N'', 901, 1, @now, N'system', @now, N'system');
END;

DECLARE @ssoClientParentId INT =
    (SELECT FunctionPermissionId FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.SsoClient');

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.SsoClient:Manage')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (@ssoClientParentId, N'System.SsoClient:Manage', N'System.SsoClient', N'SSO client 管理', N'Manage', N'管理', 1, 1, @now, N'system', @now, N'system');
END;
```

權限建立後，請透過 `Sys_RoleGroupFunctionPermission` 將 `System.SsoClient:Manage` 指派給正式環境的系統管理員角色群組。若管理員也需要維護 JWT 設定，也請一併指派 `System.JwtSetting:Manage`。

以下是指派範例，請先把角色群組名稱改成正式環境實際使用的名稱後再執行：

```sql
DECLARE @now DATETIME2(7) = SYSUTCDATETIME();

DECLARE @adminRoleGroupId INT =
    (SELECT TOP (1) RoleGroupId
     FROM dbo.Sys_RoleGroup
     WHERE RoleGroupName IN (N'系統管理員', N'Administrator', N'Administrators', N'Admin')
     ORDER BY RoleGroupId);

IF @adminRoleGroupId IS NOT NULL
BEGIN
    INSERT INTO dbo.Sys_RoleGroupFunctionPermission (RoleGroupId, FunctionPermissionId, CreatedTime, CreatedId)
    SELECT @adminRoleGroupId, p.FunctionPermissionId, @now, N'system'
    FROM dbo.Sys_FunctionPermission p
    WHERE p.PermissionKey IN (N'System.JwtSetting:Manage', N'System.SsoClient:Manage')
      AND NOT EXISTS (
          SELECT 1
          FROM dbo.Sys_RoleGroupFunctionPermission m
          WHERE m.RoleGroupId = @adminRoleGroupId
            AND m.FunctionPermissionId = p.FunctionPermissionId
      );
END;
```

## JWT 設定

SSO Server Token 有效時間由 `Sys_BasicSettings` 管理，條件為 `Type = JwtSetting`、`Key = ServerTokenExpire`。

## 日誌稽核

SSO 相關日誌分成兩個面向：

- `UserOperationLog`：管理員建立、更新、刪除 SSO Client。
- `SsoLog`：外部系統呼叫 `Login` 或 `ValidateToken` 的結果。

`SsoLog` 會記錄 `ClientId`、事件名稱、成功或失敗、來源 IP 與訊息；不記錄 `ClientSecret`、Server Token 或任何金鑰。建表語法與查詢 API 請參考 [LogService.md](../../LogService/Doc/LogService.md)。
