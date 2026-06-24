# DepartmentService 部門服務

[專案 README](../../../README.md) / [BusinessRule README](../../README.md)

## 功能說明

`DepartmentService` 負責系統部門資料管理，提供 CRUD、清單查詢與樹狀結構查詢。

- 部門清單查詢
- 部門樹狀查詢
- 單筆部門查詢
- 建立部門
- 更新部門
- 刪除部門

部門使用 `DeptId` 作為主鍵，`ParentDeptId` 表示上層部門；`ParentDeptId = null` 代表根部門。

## 檔案位置

| 類型 | 路徑 |
|---|---|
| 服務介面 | `Template.BusinessRule/DepartmentService/Services/IDepartmentService.cs` |
| Request/DTO | `Template.BusinessRule/DepartmentService/Models` |
| 服務實作 | `Template.BusinessRule/DepartmentService/Services/DepartmentService.cs` |
| API Controller | `Template.WebApi/Controllers/DepartmentController.cs` |
| 測試 | `Template.Test/Tests/DepartmentServiceTests.cs` |
| Entity | `Template.DataAccess/ProjectDbContext/Sys_Department.cs` |
| DbContext partial | `Template.DataAccess/ProjectDbContext/ProjectDbContext.Department.cs` |

## 服務方法

| 方法 | 說明 |
|---|---|
| `GetListAsync(string? keyword, bool? isEnable)` | 查詢部門清單，支援關鍵字與啟用狀態篩選 |
| `GetTreeAsync(bool? isEnable)` | 查詢部門樹狀資料 |
| `GetByIdAsync(int deptId)` | 依部門 ID 查詢單筆資料 |
| `CreateAsync(DepartmentCreateRequest request)` | 建立部門 |
| `UpdateAsync(DepartmentUpdateRequest request)` | 更新部門 |
| `DeleteAsync(int deptId)` | 刪除部門 |

## 樹狀規則

- `ParentDeptId = null` 代表根部門。
- 同層部門先依 `SortOrder` 排序，再依 `DeptId` 排序。
- 更新部門時，父部門不可設定為自己。
- 更新部門時，父部門不可設定為自己的下層部門，避免形成循環。

## 刪除規則

部門刪除前會檢查關聯資料：

- 部門底下不可有子部門。
- 部門底下不可有使用者。

## API

| API | 說明 |
|---|---|
| `GET /Department?keyword=&isEnable=` | 查詢部門清單 |
| `GET /Department/tree?isEnable=` | 查詢部門樹狀資料 |
| `GET /Department/1` | 查詢單筆部門 |
| `POST /Department` | 建立部門 |
| `PUT /Department` | 完整更新部門 |
| `PATCH /Department/1` | 局部更新部門 |
| `DELETE /Department/1` | 刪除部門 |

`PATCH /Department/1` 使用 JSON Patch 格式，後端會先讀取現有部門並套用局部異動，再交由 `UpdateAsync` 執行既有驗證與稽核。

## Request 範例

```json
{
  "DeptName": "資訊部",
  "ParentDeptId": null,
  "SortOrder": 10,
  "IsEnable": true
}
```

## Response 範例

```json
{
  "DeptId": 1,
  "DeptName": "資訊部",
  "ParentDeptId": null,
  "SortOrder": 10,
  "IsEnable": true,
  "CreatedTime": "2026-05-15T07:00:00Z",
  "CreatedId": "system",
  "UpdatedTime": "2026-05-15T07:00:00Z",
  "UpdatedId": "system",
  "Children": []
}
```

## 既有資料庫啟用部門功能

既有資料庫啟用部門功能時，請先確認 `Sys_UserInfo.DeptId` 已是 `INT` 型別，且既有使用者資料的 `DeptId` 能對應到要建立的部門資料。確認後執行下列 SQL：

```sql
IF OBJECT_ID(N'dbo.Sys_Department', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Department
    (
        DeptId INT IDENTITY(1,1) NOT NULL,
        DeptName NVARCHAR(100) NOT NULL,
        ParentDeptId INT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_Sys_Department_SortOrder DEFAULT (0),
        IsEnable BIT NOT NULL CONSTRAINT DF_Sys_Department_IsEnable DEFAULT (1),
        CreatedTime DATETIME2(7) NOT NULL CONSTRAINT DF_Sys_Department_CreatedTime DEFAULT (SYSUTCDATETIME()),
        CreatedId NVARCHAR(50) NOT NULL,
        UpdatedTime DATETIME2(7) NOT NULL CONSTRAINT DF_Sys_Department_UpdatedTime DEFAULT (SYSUTCDATETIME()),
        UpdatedId NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_Sys_Department PRIMARY KEY CLUSTERED (DeptId),
        CONSTRAINT FK_Sys_Department_ParentDeptId
            FOREIGN KEY (ParentDeptId) REFERENCES dbo.Sys_Department (DeptId)
    );

    CREATE INDEX IX_Sys_Department_ParentDeptId_SortOrder
        ON dbo.Sys_Department (ParentDeptId, SortOrder);

    CREATE INDEX IX_Sys_Department_IsEnable
        ON dbo.Sys_Department (IsEnable);
END;

IF COL_LENGTH(N'dbo.Sys_UserInfo', N'DeptId') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Sys_UserInfo')
          AND name = N'DeptId'
          AND TYPE_NAME(user_type_id) = N'int'
   )
   AND NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys
        WHERE name = N'FK_Sys_UserInfo_DeptId'
          AND parent_object_id = OBJECT_ID(N'dbo.Sys_UserInfo')
   )
BEGIN
    CREATE INDEX IX_Sys_UserInfo_DeptId
        ON dbo.Sys_UserInfo (DeptId);

    ALTER TABLE dbo.Sys_UserInfo
        ADD CONSTRAINT FK_Sys_UserInfo_DeptId
            FOREIGN KEY (DeptId) REFERENCES dbo.Sys_Department (DeptId);
END;

IF COL_LENGTH(N'dbo.Sys_UserInfo', N'DeptId') IS NOT NULL
   AND EXISTS (
        SELECT 1
        FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.Sys_UserInfo')
          AND name = N'DeptId'
          AND TYPE_NAME(user_type_id) <> N'int'
   )
BEGIN
    THROW 50001, 'Sys_UserInfo.DeptId must be INT before adding FK_Sys_UserInfo_DeptId.', 1;
END;
```

如果是空資料庫，可先建立一筆根部門，再把預設使用者的 `DeptId` 指向該部門，最後再加上 `FK_Sys_UserInfo_DeptId`。

## 日誌稽核

`DepartmentService` 會在下列人為操作寫入 `UserOperationLog`：

- 建立部門：`Action = Create`
- 更新部門：`Action = Update`
- 刪除部門：`Action = Delete`

日誌會記錄操作者、部門 ID、異動前後資料與操作訊息。建表語法與查詢 API 請參考 [LogService.md](../../LogService/Doc/LogService.md)。
