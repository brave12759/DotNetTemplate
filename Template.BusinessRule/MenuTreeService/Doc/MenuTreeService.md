# MenuTreeService 選單樹功能

[← 返回方案 README](../../../README.md) ｜ [← 返回上層 README](../../README.md)

## 功能說明

`MenuTreeService` 提供系統選單樹的查詢與維護功能。後端只管理選單節點、階層、排序與啟用狀態；前端路由與元件路徑由前端依 `MenuCode` 或自己的設定自行對應。

包含：

- 取得選單平面清單
- 取得父子階層選單樹
- 依 Id 取得選單
- 新增選單
- 更新選單
- 刪除選單

核心稽核：

- 新增、更新、刪除選單時，會寫入 `UserOperationLog`。
- `Module` 固定為 `MenuTree`，`TargetType` 為 `Sys_MenuTree`。

刪除選單時，若該選單仍有子選單，服務會拒絕刪除。更新父層時，服務會檢查不可將選單掛到自己或自己的子節點底下，避免產生循環階層。

## 相關檔案


| 類型           | 路徑                                                                |
| -------------- | ------------------------------------------------------------------- |
| 服務介面       | `Template.BusinessRule/MenuTreeService/Services/IMenuTreeService.cs` |
| 請求與輸出模型 | `Template.BusinessRule/MenuTreeService/Models`                      |
| 服務實作       | `Template.BusinessRule/MenuTreeService/Services/MenuTreeService.cs` |
| API Controller | `Template.WebApi/Controllers/MenuTreeController.cs`                 |
| 單元測試       | `Template.Test/Tests/MenuTreeServiceTests.cs`                       |
| EF Core Entity | `Template.DataAccess/ProjectDbContext/Sys_MenuTree.cs`              |
| DbContext 擴充 | `Template.DataAccess/ProjectDbContext/ProjectDbContext.MenuTree.cs` |

## API


| API                                     | 說明               |
| --------------------------------------- | ------------------ |
| `GET /MenuTree?keyword=&isEnable=` | 取得平面清單       |
| `GET /MenuTree/tree?isEnable=`          | 取得父子階層選單樹 |
| `GET /MenuTree/1`            | 依 Id 取得單筆選單 |
| `POST /MenuTree`                 | 新增選單           |
| `PUT /MenuTree`                  | 完整更新選單       |
| `PATCH /MenuTree/1`              | 局部更新選單       |
| `DELETE /MenuTree/1`          | 刪除選單           |

`PATCH /MenuTree/1` 使用 JSON Patch 格式，適合只調整名稱、排序、圖示或啟用狀態等少數欄位；套用後仍會呼叫 `UpdateAsync` 執行原本的階層循環檢查與稽核。

## DI 實裝方式

本功能不在專案中預先註冊 DI。要啟用此功能時，請在應用程式的 DI 註冊位置加入下列註冊：

```csharp
using Template.BusinessRule.MenuTreeService.Services;

builder.Services.AddScoped<IMenuTreeService, MenuTreeService>();
```

若專案仍使用 `Template.BusinessRule/Extensions/ServiceCollectionExtensions.cs` 統一註冊商業邏輯服務，也可以在 `AddBusinessRuleServices` 內加入：

```csharp
services.AddScoped<IMenuTreeService, MenuTreeService>();
```

加入前請確認已存在 `ProjectDbContext` 註冊，因為 `MenuTreeService` 會透過 `ProjectDbContext` 存取 `Sys_MenuTree`。

## 登入後回傳選單樹

正常情境下，前端通常會在登入成功後取得目前使用者可用的選單樹。本範本已在 `Template.WebApi/Controllers/AuthController.cs` 放入註解版範例，預設不啟用，避免未使用此模組的專案被迫註冊 `IMenuTreeService`。

若要啟用，請依序處理：

1. 註冊 `IMenuTreeService`。
2. 在 `AuthController.cs` 取消註解 `Template.BusinessRule.MenuTreeService.Services` 的 using。
3. 在 `AuthController` 建構子取消註解 `IMenuTreeService menuTreeService`。
4. 取消註解 `_menuTreeService` 欄位。
5. 在 `Login` 成功回傳處取消註解 `GetTreeAsync(isEnable: true)` 與包含 `MenuTree` 的回傳物件。

後端回傳的選單樹只包含選單節點、階層、排序與啟用狀態；前端路由與元件對應仍由前端自行依 `MenuCode` 或自己的設定維護。

## 移除此功能

若專案不需要選單樹功能，可移除下列檔案或資料夾：

```text
Template.BusinessRule/MenuTreeService/
Template.WebApi/Controllers/MenuTreeController.cs
Template.Test/Tests/MenuTreeServiceTests.cs
Template.DataAccess/ProjectDbContext/Sys_MenuTree.cs
Template.DataAccess/ProjectDbContext/ProjectDbContext.MenuTree.cs
```

若資料庫已建立 `Sys_MenuTree`，也請依專案資料庫版控流程移除或忽略該資料表。

## MSSQL 建表語法

```sql
CREATE TABLE [dbo].[Sys_MenuTree]
(
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ParentId] INT NULL,
    [MenuCode] NVARCHAR(100) NOT NULL,
    [MenuName] NVARCHAR(100) NOT NULL,
    [Icon] NVARCHAR(100) NOT NULL CONSTRAINT [DF_Sys_MenuTree_Icon] DEFAULT (N''),
    [SortOrder] INT NOT NULL CONSTRAINT [DF_Sys_MenuTree_SortOrder] DEFAULT (0),
    [IsEnable] BIT NOT NULL CONSTRAINT [DF_Sys_MenuTree_IsEnable] DEFAULT (1),
    [CreatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_MenuTree_CreatedTime] DEFAULT (SYSUTCDATETIME()),
    [CreatedId] NVARCHAR(50) NOT NULL,
    [UpdatedTime] DATETIME2(7) NOT NULL CONSTRAINT [DF_Sys_MenuTree_UpdatedTime] DEFAULT (SYSUTCDATETIME()),
    [UpdatedId] NVARCHAR(50) NOT NULL,
    CONSTRAINT [PK_Sys_MenuTree] PRIMARY KEY CLUSTERED ([Id] ASC),
    CONSTRAINT [UQ_Sys_MenuTree_MenuCode] UNIQUE ([MenuCode]),
    CONSTRAINT [FK_Sys_MenuTree_Parent] FOREIGN KEY ([ParentId])
        REFERENCES [dbo].[Sys_MenuTree] ([Id])
);

CREATE INDEX [IX_Sys_MenuTree_ParentId_SortOrder]
    ON [dbo].[Sys_MenuTree] ([ParentId], [SortOrder]);

CREATE INDEX [IX_Sys_MenuTree_IsEnable]
    ON [dbo].[Sys_MenuTree] ([IsEnable]);

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'系統選單樹資料表',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'主鍵',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'Id';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'父層選單 ID，NULL 表示根選單',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'ParentId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'唯一選單代碼，供前端或權限邏輯識別選單',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'MenuCode';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'顯示選單名稱',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'MenuName';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'圖示名稱',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'Icon';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'同層排序',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'SortOrder';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'啟用狀態',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'IsEnable';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立時間',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'CreatedTime';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'建立人員',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'CreatedId';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'更新時間',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'UpdatedTime';

EXEC sys.sp_addextendedproperty @name = N'MS_Description', @value = N'更新人員',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_MenuTree',
    @level2type = N'COLUMN', @level2name = N'UpdatedId';
```

時間與人員欄位統一使用下列命名：


| 欄位          | 說明     |
| ------------- | -------- |
| `CreatedTime` | 建立時間 |
| `CreatedId`   | 建立人員 |
| `UpdatedTime` | 更新時間 |
| `UpdatedId`   | 更新人員 |

## 初始資料範例

```sql
INSERT INTO [dbo].[Sys_MenuTree]
    ([ParentId], [MenuCode], [MenuName], [Icon], [SortOrder], [IsEnable], [CreatedId], [UpdatedId])
VALUES
    (NULL, N'SYSTEM', N'系統管理', N'settings', 10, 1, N'system', N'system'),
    (1, N'SYSTEM_MENU', N'選單管理', N'list-tree', 10, 1, N'system', N'system');
```
