# FileStorage 說明

[← 返回方案 README](../../../README.md) ｜ [← 返回 Template.Common README](../../README.md)

## 概述

`Template.Common/FileStorage` 提供檔案儲存核心契約，支援：

- 小檔上傳下載
- 大檔 chunk 上傳下載
- 檔案列表查詢
- 管理員/個人範圍
- 虛擬資料夾 CRUD

資料表與 SQL 腳本由 DataAccess 層承接，對應檔案為：

- `Template.DataAccess/Scripts/CreateFileStorageTables.sql`

---

## 核心資料表

- `Sys_VirtualFolder`：管理員/個人共用虛擬資料夾樹
- `Sys_Attachment`：檔案主檔（支援小檔與 chunk 大檔）

---

## 產表 SQL（含欄位描述語法）

```sql
SET NOCOUNT ON;
GO

IF OBJECT_ID(N'dbo.Sys_VirtualFolder', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_VirtualFolder
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        Scope INT NOT NULL,
        OwnerUserId NVARCHAR(50) NOT NULL,
        FolderName NVARCHAR(200) NOT NULL,
        FolderPath NVARCHAR(500) NOT NULL,
        ParentFolderId BIGINT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_Sys_VirtualFolder_SortOrder DEFAULT (0),
        IsEnable BIT NOT NULL CONSTRAINT DF_Sys_VirtualFolder_IsEnable DEFAULT (1),
        CreatedTime DATETIME2(7) NOT NULL CONSTRAINT DF_Sys_VirtualFolder_CreatedTime DEFAULT (sysutcdatetime()),
        CreatedId NVARCHAR(50) NOT NULL,
        UpdatedTime DATETIME2(7) NOT NULL CONSTRAINT DF_Sys_VirtualFolder_UpdatedTime DEFAULT (sysutcdatetime()),
        UpdatedId NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_Sys_VirtualFolder PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Sys_VirtualFolder_Parent FOREIGN KEY (ParentFolderId)
            REFERENCES dbo.Sys_VirtualFolder (Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_Sys_VirtualFolder_Scope_Owner_Path
        ON dbo.Sys_VirtualFolder(Scope, OwnerUserId, FolderPath);

    CREATE NONCLUSTERED INDEX IX_Sys_VirtualFolder_ParentFolderId_SortOrder
        ON dbo.Sys_VirtualFolder(ParentFolderId, SortOrder);
END
GO

IF OBJECT_ID(N'dbo.Sys_Attachment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sys_Attachment
    (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        FileId UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Sys_Attachment_FileId DEFAULT (newid()),
        Scope INT NOT NULL,
        OwnerUserId NVARCHAR(50) NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        Extension NVARCHAR(20) NOT NULL CONSTRAINT DF_Sys_Attachment_Extension DEFAULT (N''),
        ContentType NVARCHAR(200) NOT NULL,
        SizeBytes BIGINT NOT NULL,
        VirtualFolderId BIGINT NULL,
        FolderPath NVARCHAR(500) NOT NULL CONSTRAINT DF_Sys_Attachment_FolderPath DEFAULT (N'/'),
        StorageProvider NVARCHAR(50) NOT NULL,
        StorageKey NVARCHAR(500) NOT NULL,
        UploadMode INT NOT NULL,
        UploadStatus INT NOT NULL CONSTRAINT DF_Sys_Attachment_UploadStatus DEFAULT (1),
        IsChunked BIT NOT NULL CONSTRAINT DF_Sys_Attachment_IsChunked DEFAULT (0),
        ChunkCount INT NOT NULL CONSTRAINT DF_Sys_Attachment_ChunkCount DEFAULT (0),
        MetadataJson NVARCHAR(MAX) NOT NULL CONSTRAINT DF_Sys_Attachment_MetadataJson DEFAULT (N'{}'),
        CreatedTime DATETIME2(7) NOT NULL CONSTRAINT DF_Sys_Attachment_CreatedTime DEFAULT (sysutcdatetime()),
        CreatedId NVARCHAR(50) NOT NULL,
        UpdatedTime DATETIME2(7) NOT NULL CONSTRAINT DF_Sys_Attachment_UpdatedTime DEFAULT (sysutcdatetime()),
        UpdatedId NVARCHAR(50) NOT NULL,
        CONSTRAINT PK_Sys_Attachment PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Sys_Attachment_VirtualFolder FOREIGN KEY (VirtualFolderId)
            REFERENCES dbo.Sys_VirtualFolder (Id)
            ON DELETE SET NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX UQ_Sys_Attachment_FileId
        ON dbo.Sys_Attachment(FileId);

    CREATE UNIQUE NONCLUSTERED INDEX UQ_Sys_Attachment_StorageKey
        ON dbo.Sys_Attachment(StorageKey);

    CREATE NONCLUSTERED INDEX IX_Sys_Attachment_Scope_Owner_CreatedTime
        ON dbo.Sys_Attachment(Scope, OwnerUserId, CreatedTime DESC);

    CREATE NONCLUSTERED INDEX IX_Sys_Attachment_VirtualFolderId_CreatedTime
        ON dbo.Sys_Attachment(VirtualFolderId, CreatedTime DESC);
END
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'檔案虛擬資料夾資料表',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_VirtualFolder';
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description', @value = N'檔案主檔資料表',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'Sys_Attachment';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主鍵', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'Id';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'範圍：1 Personal、2 Admin', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'Scope';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'擁有者使用者帳號', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'OwnerUserId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'資料夾名稱', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'FolderName';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'虛擬資料夾完整路徑', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'FolderPath';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'父層資料夾 ID', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'ParentFolderId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'同層排序值', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'SortOrder';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'啟用狀態', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'IsEnable';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'建立時間', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'CreatedTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'建立人員', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'CreatedId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'更新時間', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'UpdatedTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'更新人員', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_VirtualFolder',@level2type=N'COLUMN',@level2name=N'UpdatedId';
GO

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'主鍵', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'Id';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'對外檔案識別碼', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'FileId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'範圍：1 Personal、2 Admin', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'Scope';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'擁有者使用者帳號', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'OwnerUserId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'原始檔名', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'FileName';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'副檔名（不含 .）', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'Extension';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'MIME 類型', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'ContentType';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'檔案大小（Bytes）', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'SizeBytes';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'虛擬資料夾 ID', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'VirtualFolderId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'虛擬資料夾路徑快照', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'FolderPath';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'儲存供應商名稱', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'StorageProvider';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'儲存鍵值（供應商端路徑/Key）', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'StorageKey';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'上傳模式：1 Single、2 Chunk', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'UploadMode';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'上傳狀態：1 Pending、2 Uploading、3 Ready、4 Failed、5 Deleted', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'UploadStatus';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'是否為 chunk 上傳', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'IsChunked';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'chunk 數量', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'ChunkCount';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'額外資訊 JSON', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'MetadataJson';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'建立時間', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'CreatedTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'建立人員', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'CreatedId';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'更新時間', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'UpdatedTime';
EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'更新人員', @level0type=N'SCHEMA',@level0name=N'dbo',@level1type=N'TABLE',@level1name=N'Sys_Attachment',@level2type=N'COLUMN',@level2name=N'UpdatedId';
GO
```
