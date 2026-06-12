# RoleGroupService 角色群組功能

[← 返回方案 README](../../../README.md) ｜ [← 返回上層 README](../../README.md)

## 功能說明

`RoleGroupService` 提供系統角色群組的查詢與維護功能。角色群組支援父子階層、排序、啟用狀態與使用者對應；一位使用者可以對應多個角色群組，對應資料以 `RoleGroupId` 作為角色群組 Key 值。

支援功能：

- 取得角色群組平面清單
- 取得角色群組樹狀資料
- 依 RoleGroupId 取得角色群組
- 新增角色群組
- 更新角色群組
- 刪除角色群組，並同步刪除所有子孫角色群組與使用者對應資料
- 取得指定使用者擁有的角色群組
- 以整批覆蓋方式更新指定使用者擁有的角色群組

時間欄位統一使用過去式加上 `Time`，例如 `CreatedTime`、`UpdatedTime`。

## 相關檔案

| 類型 | 路徑 |
| --- | --- |
| 服務介面 | `Template.BusinessRule/RoleGroupService/Services/IRoleGroupService.cs` |
| 訊息列舉 | `Template.BusinessRule/RoleGroupService/Enums/RoleGroupMessageEnum.cs` |
| 請求與輸出模型 | `Template.BusinessRule/RoleGroupService/Models` |
| 服務實作 | `Template.BusinessRule/RoleGroupService/Services/RoleGroupService.cs` |
| API Controller | `Template.WebApi/Controllers/RoleGroupController.cs` |
| EF Core Entity | `Template.DataAccess/ProjectDbContext/Sys_RoleGroup.cs` |
| 使用者對應 Entity | `Template.DataAccess/ProjectDbContext/Sys_UserRoleGroup.cs` |
| DbContext 擴充 | `Template.DataAccess/ProjectDbContext/ProjectDbContext.RoleGroup.cs` |

## API

| API | 說明 |
| --- | --- |
| `GET /RoleGroup/List?keyword=&isEnable=` | 取得角色群組平面清單 |
| `GET /RoleGroup/Tree?isEnable=` | 取得角色群組樹狀資料 |
| `GET /RoleGroup/GetById?roleGroupId=1` | 依 RoleGroupId 取得單筆角色群組 |
| `POST /RoleGroup/Create` | 新增角色群組 |
| `PUT /RoleGroup/Update` | 更新角色群組 |
| `DELETE /RoleGroup/Delete?roleGroupId=1` | 刪除角色群組 |
| `GET /RoleGroup/User?userId=admin&isEnable=true` | 取得指定使用者擁有的角色群組 |
| `PUT /RoleGroup/UpdateUser` | 更新指定使用者擁有的角色群組 |

## DI 註冊方式

本服務不會在共用註冊流程中直接註冊。需要啟用角色群組功能時，請自行在 WebApi 的 DI 設定加入：

```csharp
using Template.BusinessRule.RoleGroupService.Services;

builder.Services.AddScoped<IRoleGroupService, RoleGroupService>();
```

若要集中在 `Template.BusinessRule/Extensions/ServiceCollectionExtensions.cs` 註冊，也可以在專案確認要啟用此模組後加入：

```csharp
services.AddScoped<IRoleGroupService, RoleGroupService>();
```

加入前請確認已存在 `ProjectDbContext` 註冊，因為 `RoleGroupService` 會透過 `ProjectDbContext` 存取 `Sys_RoleGroup` 與 `Sys_UserRoleGroup`。

## 登入成功後回傳角色群組

正常情境下，前端可能會在登入成功後取得目前使用者擁有的角色群組。本範本已在 `Template.WebApi/Controllers/AuthController.cs` 放入註解版範例，預設不啟用，避免未使用此模組的專案被迫註冊 `IRoleGroupService`。

需要啟用時，請依序執行：

1. 註冊 `IRoleGroupService`。
2. 在 `AuthController.cs` 取消註解 `Template.BusinessRule.RoleGroupService.Services` 的 using。
3. 在 `AuthController` 建構子取消註解 `IRoleGroupService roleGroupService`。
4. 取消註解 `_roleGroupService` 欄位。
5. 在 `Login` 成功回傳處取消註解 `GetUserRoleGroupsAsync(request.UserId, isEnable: true)` 與包含 `RoleGroups` 的回傳物件。

後端回傳的角色群組只包含角色群組節點、階層、排序與啟用狀態；前端各功能請依 `RoleGroupId` 對應。

## 移除方式

若專案不需要角色群組功能，可移除下列檔案：

```text
Template.BusinessRule/RoleGroupService/
Template.WebApi/Controllers/RoleGroupController.cs
Template.DataAccess/ProjectDbContext/Sys_RoleGroup.cs
Template.DataAccess/ProjectDbContext/Sys_UserRoleGroup.cs
Template.DataAccess/ProjectDbContext/ProjectDbContext.RoleGroup.cs
```

若資料庫已建立 `Sys_RoleGroup` 或 `Sys_UserRoleGroup`，也請依專案資料庫版控流程移除或忽略該資料表。

## MSSQL 建表語法

```sql
CREATE TABLE [dbo].[Sys_RoleGroup]
(
    [RoleGroupId] INT IDENTITY(1,1) NOT NULL,
    [ParentRoleGroupId] INT NULL,
    [RoleGroupName] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NOT NULL CONSTRAINT [DF_Sys_RoleGroup_Description] DEFAULT (N''),
    [SortOrder] INT NOT NULL CONSTRAINT [DF_Sys_RoleGroup_SortOrder] DEFAULT (0),
    [IsEnable] BIT NOT NULL CONSTRAINT [DF_Sys_RoleGroup_IsEnable] DEFAULT (1),
    [CreatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_RoleGroup_CreatedTime] DEFAULT (SYSUTCDATETIME()),
    [CreatedId] NVARCHAR(50) NOT NULL,
    [UpdatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_RoleGroup_UpdatedTime] DEFAULT (SYSUTCDATETIME()),
    [UpdatedId] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Sys_RoleGroup] PRIMARY KEY CLUSTERED ([RoleGroupId] ASC),
    CONSTRAINT [FK_Sys_RoleGroup_Parent] FOREIGN KEY ([ParentRoleGroupId])
        REFERENCES [dbo].[Sys_RoleGroup] ([RoleGroupId])
);

CREATE INDEX [IX_Sys_RoleGroup_ParentRoleGroupId_SortOrder]
    ON [dbo].[Sys_RoleGroup] ([ParentRoleGroupId], [SortOrder]);

CREATE INDEX [IX_Sys_RoleGroup_IsEnable]
    ON [dbo].[Sys_RoleGroup] ([IsEnable]);

CREATE TABLE [dbo].[Sys_UserRoleGroup]
(
    [UserId] NVARCHAR(50) NOT NULL,
    [RoleGroupId] INT NOT NULL,
    [CreatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_UserRoleGroup_CreatedTime] DEFAULT (SYSUTCDATETIME()),
    [CreatedId] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Sys_UserRoleGroup] PRIMARY KEY CLUSTERED ([UserId] ASC, [RoleGroupId] ASC),
    CONSTRAINT [FK_Sys_UserRoleGroup_User] FOREIGN KEY ([UserId])
        REFERENCES [dbo].[Sys_UserInfo] ([UserId])
        ON DELETE CASCADE,
    CONSTRAINT [FK_Sys_UserRoleGroup_RoleGroup] FOREIGN KEY ([RoleGroupId])
        REFERENCES [dbo].[Sys_RoleGroup] ([RoleGroupId])
        ON DELETE CASCADE
);

CREATE INDEX [IX_Sys_UserRoleGroup_RoleGroupId]
    ON [dbo].[Sys_UserRoleGroup] ([RoleGroupId]);

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'系統角色群組資料表',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'角色群組 ID',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'RoleGroupId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'上層角色群組 ID，NULL 代表根節點',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'ParentRoleGroupId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'角色群組名稱',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'RoleGroupName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'角色群組描述',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'Description';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'排序值',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'SortOrder';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'啟用狀態',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'IsEnable';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立時間',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'CreatedTime';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立人員',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'CreatedId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'更新時間',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'UpdatedTime';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'更新人員',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroup',
    @level2type = N'COLUMN', @level2name = N'UpdatedId';

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'使用者角色群組對應表',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_UserRoleGroup';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'使用者帳號',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_UserRoleGroup',
    @level2type = N'COLUMN', @level2name = N'UserId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'角色群組 ID',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_UserRoleGroup',
    @level2type = N'COLUMN', @level2name = N'RoleGroupId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立時間',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_UserRoleGroup',
    @level2type = N'COLUMN', @level2name = N'CreatedTime';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立人員',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_UserRoleGroup',
    @level2type = N'COLUMN', @level2name = N'CreatedId';
```

稽核欄位命名統一如下：

| 欄位 | 說明 |
| --- | --- |
| `CreatedTime` | 建立時間 |
| `CreatedId` | 建立人員 |
| `UpdatedTime` | 更新時間 |
| `UpdatedId` | 更新人員 |

## 範例資料語法

```sql
INSERT INTO [dbo].[Sys_RoleGroup]
    ([ParentRoleGroupId], [RoleGroupName], [Description], [SortOrder], [IsEnable], [CreatedId], [UpdatedId])
VALUES
    (NULL, N'系統管理員', N'系統管理相關權限群組', 10, 1, N'system', N'system'),
    (NULL, N'一般使用者', N'一般操作權限群組', 20, 1, N'system', N'system');

INSERT INTO [dbo].[Sys_UserRoleGroup]
    ([UserId], [RoleGroupId], [CreatedId])
SELECT N'admin', [RoleGroupId], N'system'
FROM [dbo].[Sys_RoleGroup]
WHERE [RoleGroupName] IN (N'系統管理員', N'一般使用者');
```

## 日誌稽核

`RoleGroupService` 會在下列人為操作寫入 `UserOperationLog`：

- 建立角色群組：`Action = Create`
- 更新角色群組：`Action = Update`
- 刪除角色群組與子角色群組：`Action = Delete`
- 更新使用者角色群組：`Action = PermissionChange`

日誌會記錄操作者、角色群組 ID、被指派的使用者與異動前後資料。建表語法與查詢 API 請參考 [LogService.md](../../LogService/Doc/LogService.md)。
