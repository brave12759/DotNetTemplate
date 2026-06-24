# LogService 日誌服務

[專案 README](../../../README.md) / [BusinessRule README](../../README.md)

## 功能說明

`LogService` 是日誌寫入入口，透過 `ILogWriterFactory` 依日誌種類取得 writer。日誌不做成單一大表，各面向各自有資料表：

- `UserOperationLog`：使用者、登入、部門、RoleGroup、SSO Client 管理等人為操作。
- `QueueLog`：佇列建立、領取、完成、失敗等執行紀錄。
- `SsoLog`：SSO Login 與 Token 驗證紀錄。

## API

| API | 權限 | 說明 |
|---|---|---|
| `GET /Log/user-operation-logs` | `System.UserOperationLog:View` | 查詢使用者操作日誌 |
| `GET /Log/queue-logs` | `System.QueueLog:View` | 查詢佇列日誌 |
| `GET /Log/sso-logs` | `System.SsoLog:View` | 查詢 SSO 日誌 |

三個查詢 API 皆支援基本參數：

- `startTime`
- `endTime`
- `operatorId`
- `page`
- `pageSize`

## 已接入範圍

- `UserService`：建立、更新、刪除、重設密碼、變更密碼。
- `LoginService`：登入成功/失敗、帳號鎖定、密碼過期、Token 刷新、登出。
- `DepartmentService`：建立、更新、刪除部門。
- `RoleGroupService`：建立、更新、刪除角色群組、更新使用者角色群組。
- `SsoService`：SSO Client 管理寫入 `UserOperationLog`，SSO Login/ValidateToken 寫入 `SsoLog`。
- `DbBackgroundTaskQueue`：Enqueue、Claim、Complete、Fail 寫入 `QueueLog`。

密碼、Token、ClientSecret、金鑰等敏感資料不可寫入日誌。

## 建表語法

既有資料庫啟用日誌功能時，請在 Log 資料庫執行下列 SQL。

```sql
IF OBJECT_ID(N'dbo.UserOperationLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserOperationLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        EventTime DATETIME2(7) NOT NULL,
        UserId NVARCHAR(50) NOT NULL CONSTRAINT DF_UserOperationLog_UserId DEFAULT (N''),
        Module NVARCHAR(100) NOT NULL CONSTRAINT DF_UserOperationLog_Module DEFAULT (N''),
        Action INT NOT NULL,
        Result INT NOT NULL,
        TargetType NVARCHAR(100) NOT NULL CONSTRAINT DF_UserOperationLog_TargetType DEFAULT (N''),
        TargetId NVARCHAR(200) NOT NULL CONSTRAINT DF_UserOperationLog_TargetId DEFAULT (N''),
        IpAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_UserOperationLog_IpAddress DEFAULT (N''),
        TraceId NVARCHAR(100) NOT NULL CONSTRAINT DF_UserOperationLog_TraceId DEFAULT (N''),
        Message NVARCHAR(MAX) NOT NULL CONSTRAINT DF_UserOperationLog_Message DEFAULT (N''),
        OldValueJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_UserOperationLog_OldValueJson DEFAULT (N''),
        NewValueJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_UserOperationLog_NewValueJson DEFAULT (N''),
        MetadataJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_UserOperationLog_MetadataJson DEFAULT (N''),
        CONSTRAINT PK_UserOperationLog PRIMARY KEY CLUSTERED (Id)
    );
END;

IF OBJECT_ID(N'dbo.QueueLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QueueLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        EventTime DATETIME2(7) NOT NULL,
        OperatorId NVARCHAR(50) NOT NULL CONSTRAINT DF_QueueLog_OperatorId DEFAULT (N''),
        JobId BIGINT NOT NULL,
        WorkType INT NOT NULL,
        WorkKey NVARCHAR(200) NOT NULL CONSTRAINT DF_QueueLog_WorkKey DEFAULT (N''),
        EventName NVARCHAR(50) NOT NULL CONSTRAINT DF_QueueLog_EventName DEFAULT (N''),
        Status INT NOT NULL,
        RetryCount INT NOT NULL,
        Message NVARCHAR(MAX) NOT NULL CONSTRAINT DF_QueueLog_Message DEFAULT (N''),
        ErrorMessage NVARCHAR(MAX) NOT NULL CONSTRAINT DF_QueueLog_ErrorMessage DEFAULT (N''),
        MetadataJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_QueueLog_MetadataJson DEFAULT (N''),
        CONSTRAINT PK_QueueLog PRIMARY KEY CLUSTERED (Id)
    );
END;

IF OBJECT_ID(N'dbo.SsoLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SsoLog
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        EventTime DATETIME2(7) NOT NULL,
        OperatorId NVARCHAR(100) NOT NULL CONSTRAINT DF_SsoLog_OperatorId DEFAULT (N''),
        ClientId NVARCHAR(100) NOT NULL CONSTRAINT DF_SsoLog_ClientId DEFAULT (N''),
        EventName NVARCHAR(50) NOT NULL CONSTRAINT DF_SsoLog_EventName DEFAULT (N''),
        Result NVARCHAR(20) NOT NULL CONSTRAINT DF_SsoLog_Result DEFAULT (N''),
        IpAddress NVARCHAR(50) NOT NULL CONSTRAINT DF_SsoLog_IpAddress DEFAULT (N''),
        Message NVARCHAR(MAX) NOT NULL CONSTRAINT DF_SsoLog_Message DEFAULT (N''),
        MetadataJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_SsoLog_MetadataJson DEFAULT (N''),
        CONSTRAINT PK_SsoLog PRIMARY KEY CLUSTERED (Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserOperationLog_EventTime' AND object_id = OBJECT_ID(N'dbo.UserOperationLog'))
    CREATE INDEX IX_UserOperationLog_EventTime ON dbo.UserOperationLog (EventTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserOperationLog_UserId_EventTime' AND object_id = OBJECT_ID(N'dbo.UserOperationLog'))
    CREATE INDEX IX_UserOperationLog_UserId_EventTime ON dbo.UserOperationLog (UserId, EventTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserOperationLog_Module_Action_EventTime' AND object_id = OBJECT_ID(N'dbo.UserOperationLog'))
    CREATE INDEX IX_UserOperationLog_Module_Action_EventTime ON dbo.UserOperationLog (Module, Action, EventTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QueueLog_EventTime' AND object_id = OBJECT_ID(N'dbo.QueueLog'))
    CREATE INDEX IX_QueueLog_EventTime ON dbo.QueueLog (EventTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QueueLog_OperatorId_EventTime' AND object_id = OBJECT_ID(N'dbo.QueueLog'))
    CREATE INDEX IX_QueueLog_OperatorId_EventTime ON dbo.QueueLog (OperatorId, EventTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_QueueLog_JobId_EventTime' AND object_id = OBJECT_ID(N'dbo.QueueLog'))
    CREATE INDEX IX_QueueLog_JobId_EventTime ON dbo.QueueLog (JobId, EventTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SsoLog_EventTime' AND object_id = OBJECT_ID(N'dbo.SsoLog'))
    CREATE INDEX IX_SsoLog_EventTime ON dbo.SsoLog (EventTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SsoLog_OperatorId_EventTime' AND object_id = OBJECT_ID(N'dbo.SsoLog'))
    CREATE INDEX IX_SsoLog_OperatorId_EventTime ON dbo.SsoLog (OperatorId, EventTime);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_SsoLog_ClientId_EventTime' AND object_id = OBJECT_ID(N'dbo.SsoLog'))
    CREATE INDEX IX_SsoLog_ClientId_EventTime ON dbo.SsoLog (ClientId, EventTime);
```

## 權限語法

若專案啟用功能權限，請在主資料庫補上下列查詢權限，並指派給系統管理員角色群組。

```sql
DECLARE @now DATETIME2(7) = SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.UserOperationLog')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (NULL, N'System.UserOperationLog', N'System.UserOperationLog', N'使用者操作日誌', NULL, N'', 900, 1, @now, N'system', @now, N'system');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.QueueLog')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (NULL, N'System.QueueLog', N'System.QueueLog', N'佇列日誌', NULL, N'', 910, 1, @now, N'system', @now, N'system');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.SsoLog')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (NULL, N'System.SsoLog', N'System.SsoLog', N'SSO 日誌', NULL, N'', 920, 1, @now, N'system', @now, N'system');
END;

DECLARE @userOperationLogId INT = (SELECT FunctionPermissionId FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.UserOperationLog');
DECLARE @queueLogId INT = (SELECT FunctionPermissionId FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.QueueLog');
DECLARE @ssoLogId INT = (SELECT FunctionPermissionId FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.SsoLog');

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.UserOperationLog:View')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (@userOperationLogId, N'System.UserOperationLog:View', N'System.UserOperationLog', N'使用者操作日誌', N'View', N'查看', 10, 1, @now, N'system', @now, N'system');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.QueueLog:View')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (@queueLogId, N'System.QueueLog:View', N'System.QueueLog', N'佇列日誌', N'View', N'查看', 10, 1, @now, N'system', @now, N'system');
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Sys_FunctionPermission WHERE PermissionKey = N'System.SsoLog:View')
BEGIN
    INSERT INTO dbo.Sys_FunctionPermission
        (ParentFunctionPermissionId, PermissionKey, FunctionCode, FunctionName, OperationCode, OperationName, SortOrder, IsEnable, CreatedTime, CreatedId, UpdatedTime, UpdatedId)
    VALUES
        (@ssoLogId, N'System.SsoLog:View', N'System.SsoLog', N'SSO 日誌', N'View', N'查看', 10, 1, @now, N'system', @now, N'system');
END;
```
