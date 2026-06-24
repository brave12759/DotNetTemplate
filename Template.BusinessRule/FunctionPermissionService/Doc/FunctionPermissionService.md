# FunctionPermissionService 功能操作權限

[← 返回方案 README](../../../README.md) ｜ [← 返回上層 README](../../README.md)

## 功能說明

`FunctionPermissionService` 提供系統功能操作權限的查詢、維護、樹狀資料、角色群組指派與一鍵補足功能。功能操作權限以功能節點為父層，操作權限為子層；目前基本操作規劃為 `CRUDAF`。

基本操作代碼：

| 代碼 | 說明 |
| --- | --- |
| `C` | 新增 |
| `R` | 讀取 |
| `U` | 更新 |
| `D` | 刪除 |
| `A` | 審核 |
| `F` | 檔案上傳 / 下載 |

支援功能：

- 取得功能操作權限平面清單
- 取得功能操作權限樹狀資料
- 依 FunctionPermissionId 取得功能操作權限
- 新增功能操作權限
- 更新功能操作權限
- 刪除功能操作權限，並同步刪除所有子孫權限與角色群組對應資料
- 依目前選單樹一鍵補足功能節點與 `CRUDAF` 操作節點
- 取得指定角色群組已指派的功能操作權限清單
- 取得指定角色群組已指派的功能操作權限樹
- 以整批覆蓋方式更新指定角色群組的功能操作權限
- 取得指定使用者經由角色群組取得的功能操作權限樹

核心稽核：

- 新增、更新、刪除功能操作權限時，會寫入 `UserOperationLog`。
- `Module` 固定為 `FunctionPermission`，`TargetType` 為 `Sys_FunctionPermission`。

此功能在實務上通常會高度綁定 `RoleGroupService`。`Sys_RoleGroupFunctionPermission` 以 `RoleGroupId` 與 `FunctionPermissionId` 建立對應，登入後若要回傳目前使用者可用的功能操作權限樹，會透過使用者擁有的角色群組彙整權限。

時間欄位統一使用過去式加上 `Time`，例如 `CreatedTime`、`UpdatedTime`。

## 相關檔案

| 類型 | 路徑 |
| --- | --- |
| 服務介面 | `Template.BusinessRule/FunctionPermissionService/Services/IFunctionPermissionService.cs` |
| 訊息列舉 | `Template.BusinessRule/FunctionPermissionService/Enums/FunctionPermissionMessageEnum.cs` |
| 操作代碼列舉 | `Template.BusinessRule/FunctionPermissionService/Enums/FunctionOperationCode.cs` |
| 請求與輸出模型 | `Template.BusinessRule/FunctionPermissionService/Models` |
| 服務實作 | `Template.BusinessRule/FunctionPermissionService/Services/FunctionPermissionService.cs` |
| API Controller | `Template.WebApi/Controllers/FunctionPermissionController.cs` |
| EF Core Entity | `Template.DataAccess/ProjectDbContext/Sys_FunctionPermission.cs` |
| 角色群組對應 Entity | `Template.DataAccess/ProjectDbContext/Sys_RoleGroupFunctionPermission.cs` |
| DbContext 擴充 | `Template.DataAccess/ProjectDbContext/ProjectDbContext.FunctionPermission.cs` |
| 單元測試 | `Template.Test/Tests/FunctionPermissionServiceTests.cs` |

## API

| API | 說明 |
| --- | --- |
| `GET /FunctionPermission?keyword=&isEnable=` | 取得功能操作權限平面清單 |
| `GET /FunctionPermission/tree?isEnable=` | 取得功能操作權限樹狀資料 |
| `GET /FunctionPermission/1` | 依 FunctionPermissionId 取得單筆功能操作權限 |
| `POST /FunctionPermission` | 新增功能操作權限 |
| `PUT /FunctionPermission` | 完整更新功能操作權限 |
| `PATCH /FunctionPermission/1` | 局部更新功能操作權限 |
| `DELETE /FunctionPermission/1` | 刪除功能操作權限 |
| `POST /FunctionPermission/sync-from-menu-tree?includeDisabledMenus=false` | 依目前選單樹一鍵補足功能操作權限 |
| `GET /FunctionPermission/role-groups/1/permissions?isEnable=true` | 取得指定角色群組已指派的功能操作權限清單 |
| `GET /FunctionPermission/role-groups/1/tree?isEnable=true` | 取得指定角色群組已指派的功能操作權限樹 |
| `PUT /FunctionPermission/role-groups/permissions` | 更新指定角色群組的功能操作權限 |
| `GET /FunctionPermission/users/admin/tree?isEnable=true` | 取得指定使用者經由角色群組取得的功能操作權限樹 |

`PATCH /FunctionPermission/1` 使用 JSON Patch 格式，適合局部調整功能名稱、排序或啟用狀態；角色群組權限指派屬於整批覆蓋操作，仍使用 `PUT /FunctionPermission/role-groups/permissions`。

## DI 註冊方式

本服務不會在共用註冊流程中直接註冊。需要啟用功能操作權限時，請自行在 WebApi 的 DI 設定加入：

```csharp
using Template.BusinessRule.FunctionPermissionService.Services;

builder.Services.AddScoped<IFunctionPermissionService, FunctionPermissionService>();
```

若要集中在 `Template.BusinessRule/Extensions/ServiceCollectionExtensions.cs` 註冊，也可以在專案確認要啟用此模組後加入：

```csharp
services.AddScoped<IFunctionPermissionService, FunctionPermissionService>();
```

加入前請確認已存在 `ProjectDbContext` 註冊，且若要使用角色群組指派或登入權限樹，資料庫也需建立 `Sys_RoleGroup` 與 `Sys_UserRoleGroup`。

## 一鍵補足功能操作權限

`SyncFromMenuTreeAsync` 會依目前 `Sys_MenuTree` 資料自動補足 `Sys_FunctionPermission`：

- 每一筆選單會產生或更新一筆功能父節點，`PermissionKey` 與 `FunctionCode` 使用 `MenuCode`。
- 功能節點會依 `Sys_MenuTree.ParentId` 保留選單父子階層。
- 每一筆選單會產生或更新 `C`、`R`、`U`、`D`、`A`、`F` 六筆操作子節點。
- 既有資料會更新名稱、排序與啟用狀態。
- 不會自動刪除已不存在於選單樹的功能操作權限，避免誤刪專案自行建立的權限。

## 登入成功後回傳功能操作權限樹

正常情境下，前端可能會在登入成功後取得目前使用者可用的功能操作權限樹。本範本已在 `Template.WebApi/Controllers/AuthController.cs` 放入註解版範例，預設不啟用，避免未使用此模組的專案被迫註冊 `IFunctionPermissionService`。

此功能通常會高度綁定角色群組，因為使用者的功能操作權限會透過 `Sys_UserRoleGroup` 取得使用者擁有的角色群組，再透過 `Sys_RoleGroupFunctionPermission` 彙整權限。

需要啟用時，請依序執行：

1. 註冊 `IFunctionPermissionService`。
2. 確認角色群組資料表與角色群組權限對應資料表已建立。
3. 在 `AuthController.cs` 取消註解 `Template.BusinessRule.FunctionPermissionService.Services` 的 using。
4. 在 `AuthController` 建構子取消註解 `IFunctionPermissionService functionPermissionService`。
5. 取消註解 `_functionPermissionService` 欄位。
6. 在 `Login` 成功回傳處取消註解 `GetUserPermissionTreeAsync(request.UserId, isEnable: true)` 與包含 `FunctionPermissionTree` 的回傳物件。

後端回傳的功能操作權限樹只包含功能節點、操作節點、階層、排序與啟用狀態；前端各功能請依 `PermissionKey` 或 `FunctionCode` + `OperationCode` 對應。

## 移除方式

若專案不需要功能操作權限，可移除下列檔案：

```text
Template.BusinessRule/FunctionPermissionService/
Template.WebApi/Controllers/FunctionPermissionController.cs
Template.Test/Tests/FunctionPermissionServiceTests.cs
Template.DataAccess/ProjectDbContext/Sys_FunctionPermission.cs
Template.DataAccess/ProjectDbContext/Sys_RoleGroupFunctionPermission.cs
Template.DataAccess/ProjectDbContext/ProjectDbContext.FunctionPermission.cs
```

若資料庫已建立 `Sys_FunctionPermission` 或 `Sys_RoleGroupFunctionPermission`，也請依專案資料庫版控流程移除或忽略該資料表。

## MSSQL 建表語法

```sql
CREATE TABLE [dbo].[Sys_FunctionPermission]
(
    [FunctionPermissionId] INT IDENTITY(1,1) NOT NULL,
    [ParentFunctionPermissionId] INT NULL,
    [PermissionKey] NVARCHAR(150) NOT NULL,
    [FunctionCode] NVARCHAR(100) NOT NULL,
    [FunctionName] NVARCHAR(100) NOT NULL,
    [OperationCode] NVARCHAR(10) NULL,
    [OperationName] NVARCHAR(50) NOT NULL CONSTRAINT [DF_Sys_FunctionPermission_OperationName] DEFAULT (N''),
    [SortOrder] INT NOT NULL CONSTRAINT [DF_Sys_FunctionPermission_SortOrder] DEFAULT (0),
    [IsEnable] BIT NOT NULL CONSTRAINT [DF_Sys_FunctionPermission_IsEnable] DEFAULT (1),
    [CreatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_FunctionPermission_CreatedTime] DEFAULT (SYSUTCDATETIME()),
    [CreatedId] NVARCHAR(50) NOT NULL,
    [UpdatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_FunctionPermission_UpdatedTime] DEFAULT (SYSUTCDATETIME()),
    [UpdatedId] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Sys_FunctionPermission] PRIMARY KEY CLUSTERED ([FunctionPermissionId] ASC),
    CONSTRAINT [UQ_Sys_FunctionPermission_PermissionKey] UNIQUE ([PermissionKey]),
    CONSTRAINT [FK_Sys_FunctionPermission_Parent] FOREIGN KEY ([ParentFunctionPermissionId])
        REFERENCES [dbo].[Sys_FunctionPermission] ([FunctionPermissionId])
);

CREATE INDEX [IX_Sys_FunctionPermission_ParentFunctionPermissionId_SortOrder]
    ON [dbo].[Sys_FunctionPermission] ([ParentFunctionPermissionId], [SortOrder]);

CREATE INDEX [IX_Sys_FunctionPermission_FunctionCode_OperationCode]
    ON [dbo].[Sys_FunctionPermission] ([FunctionCode], [OperationCode]);

CREATE INDEX [IX_Sys_FunctionPermission_IsEnable]
    ON [dbo].[Sys_FunctionPermission] ([IsEnable]);

CREATE TABLE [dbo].[Sys_RoleGroupFunctionPermission]
(
    [RoleGroupId] INT NOT NULL,
    [FunctionPermissionId] INT NOT NULL,
    [CreatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_RoleGroupFunctionPermission_CreatedTime] DEFAULT (SYSUTCDATETIME()),
    [CreatedId] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Sys_RoleGroupFunctionPermission] PRIMARY KEY CLUSTERED ([RoleGroupId] ASC, [FunctionPermissionId] ASC),
    CONSTRAINT [FK_Sys_RoleGroupFunctionPermission_RoleGroup] FOREIGN KEY ([RoleGroupId])
        REFERENCES [dbo].[Sys_RoleGroup] ([RoleGroupId])
        ON DELETE CASCADE,
    CONSTRAINT [FK_Sys_RoleGroupFunctionPermission_FunctionPermission] FOREIGN KEY ([FunctionPermissionId])
        REFERENCES [dbo].[Sys_FunctionPermission] ([FunctionPermissionId])
        ON DELETE CASCADE
);

CREATE INDEX [IX_Sys_RoleGroupFunctionPermission_FunctionPermissionId]
    ON [dbo].[Sys_RoleGroupFunctionPermission] ([FunctionPermissionId]);

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'系統功能操作權限資料表',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'功能操作權限 ID',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'FunctionPermissionId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'上層功能操作權限 ID，NULL 代表根節點',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'ParentFunctionPermissionId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'權限鍵值，功能節點使用 FunctionCode，操作節點使用 FunctionCode:OperationCode',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'PermissionKey';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'功能代碼',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'FunctionCode';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'功能名稱',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'FunctionName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'操作代碼：C 新增、R 讀取、U 更新、D 刪除、A 審核、F 檔案上傳/下載；NULL 代表功能節點',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'OperationCode';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'操作名稱',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'OperationName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'排序值',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'SortOrder';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'啟用狀態',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'IsEnable';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立時間',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'CreatedTime';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立人員',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'CreatedId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'更新時間',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'UpdatedTime';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'更新人員',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_FunctionPermission',
    @level2type = N'COLUMN', @level2name = N'UpdatedId';

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'角色群組功能操作權限對應表',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroupFunctionPermission';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'角色群組 ID',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroupFunctionPermission',
    @level2type = N'COLUMN', @level2name = N'RoleGroupId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'功能操作權限 ID',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroupFunctionPermission',
    @level2type = N'COLUMN', @level2name = N'FunctionPermissionId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立時間',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroupFunctionPermission',
    @level2type = N'COLUMN', @level2name = N'CreatedTime';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立人員',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_RoleGroupFunctionPermission',
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

可直接透過 API 依選單樹補足功能操作權限：

```http
POST /FunctionPermission/sync-from-menu-tree?includeDisabledMenus=false
```

若要手動建立基本功能節點與操作節點：

```sql
INSERT INTO [dbo].[Sys_FunctionPermission]
    ([ParentFunctionPermissionId], [PermissionKey], [FunctionCode], [FunctionName], [OperationCode], [OperationName], [SortOrder], [IsEnable], [CreatedId], [UpdatedId])
VALUES
    (NULL, N'USER', N'USER', N'使用者管理', NULL, N'', 10, 1, N'system', N'system');

DECLARE @UserFunctionPermissionId INT = SCOPE_IDENTITY();

INSERT INTO [dbo].[Sys_FunctionPermission]
    ([ParentFunctionPermissionId], [PermissionKey], [FunctionCode], [FunctionName], [OperationCode], [OperationName], [SortOrder], [IsEnable], [CreatedId], [UpdatedId])
VALUES
    (@UserFunctionPermissionId, N'USER:C', N'USER', N'使用者管理', N'C', N'新增', 1, 1, N'system', N'system'),
    (@UserFunctionPermissionId, N'USER:R', N'USER', N'使用者管理', N'R', N'讀取', 2, 1, N'system', N'system'),
    (@UserFunctionPermissionId, N'USER:U', N'USER', N'使用者管理', N'U', N'更新', 3, 1, N'system', N'system'),
    (@UserFunctionPermissionId, N'USER:D', N'USER', N'使用者管理', N'D', N'刪除', 4, 1, N'system', N'system'),
    (@UserFunctionPermissionId, N'USER:A', N'USER', N'使用者管理', N'A', N'審核', 5, 1, N'system', N'system'),
    (@UserFunctionPermissionId, N'USER:F', N'USER', N'使用者管理', N'F', N'檔案上傳/下載', 6, 1, N'system', N'system');
```
