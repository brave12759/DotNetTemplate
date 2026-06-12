using Template.Common.Models;

namespace Template.Common.FileStorage;

/// <summary>
/// 檔案檢視範圍。
/// </summary>
public enum FileScope
{
    Personal = 1,
    Admin = 2
}

/// <summary>
/// 檔案上傳模式。
/// </summary>
public enum FileUploadMode
{
    Single = 1,
    Chunk = 2
}

/// <summary>
/// 單檔上傳請求。
/// </summary>
public class SingleFileUploadRequest
{
    public string RequestUserId { get; set; } = string.Empty;
    public FileScope Scope { get; set; } = FileScope.Personal;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long ContentLength { get; set; }
    public Stream Content { get; set; } = Stream.Null;
    public string? FolderPath { get; set; }
    public string[] Tags { get; set; } = [];
}

/// <summary>
/// 初始化 chunk 上傳請求。
/// </summary>
public class ChunkUploadInitRequest
{
    public string RequestUserId { get; set; } = string.Empty;
    public FileScope Scope { get; set; } = FileScope.Personal;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long TotalSize { get; set; }
    public int ChunkSize { get; set; }
    public int TotalChunks { get; set; }
    public string? FolderPath { get; set; }
    public string[] Tags { get; set; } = [];
}

/// <summary>
/// chunk 上傳會話資訊。
/// </summary>
public class ChunkUploadSession
{
    public string UploadId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long TotalSize { get; set; }
    public int ChunkSize { get; set; }
    public int TotalChunks { get; set; }
    public int UploadedChunks { get; set; }
}

/// <summary>
/// 上傳單一 chunk。
/// </summary>
public class ChunkUploadPartRequest
{
    public string UploadId { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public long ChunkSize { get; set; }
    public Stream Content { get; set; } = Stream.Null;
    public string? Checksum { get; set; }
}

/// <summary>
/// 完成 chunk 上傳。
/// </summary>
public class ChunkUploadCompleteRequest
{
    public string UploadId { get; set; } = string.Empty;
    public int[] ChunkIndexes { get; set; } = [];
}

/// <summary>
/// 下載檔案請求。
/// </summary>
public class FileDownloadRequest
{
    public string RequestUserId { get; set; } = string.Empty;
    public FileScope Scope { get; set; } = FileScope.Personal;
    public string FileId { get; set; } = string.Empty;
}

/// <summary>
/// 檔案列表查詢。
/// </summary>
public class FileListQueryRequest
{
    public string RequestUserId { get; set; } = string.Empty;
    public FileScope Scope { get; set; } = FileScope.Personal;
    public string? FolderPath { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 檔案刪除請求。
/// </summary>
public class FileDeleteRequest
{
    public string RequestUserId { get; set; } = string.Empty;
    public FileScope Scope { get; set; } = FileScope.Personal;
    public string FileId { get; set; } = string.Empty;
}

/// <summary>
/// 建立虛擬資料夾請求。
/// </summary>
public class VirtualFolderCreateRequest
{
    public string RequestUserId { get; set; } = string.Empty;
    public FileScope Scope { get; set; } = FileScope.Personal;
    public string FolderName { get; set; } = string.Empty;
    public string? ParentPath { get; set; }
}

/// <summary>
/// 更新虛擬資料夾請求（改名/搬移）。
/// </summary>
public class VirtualFolderUpdateRequest
{
    public string RequestUserId { get; set; } = string.Empty;
    public FileScope Scope { get; set; } = FileScope.Personal;
    public string FolderPath { get; set; } = string.Empty;
    public string? NewFolderName { get; set; }
    public string? NewParentPath { get; set; }
}

/// <summary>
/// 刪除虛擬資料夾請求。
/// </summary>
public class VirtualFolderDeleteRequest
{
    public string RequestUserId { get; set; } = string.Empty;
    public FileScope Scope { get; set; } = FileScope.Personal;
    public string FolderPath { get; set; } = string.Empty;
    public bool Recursive { get; set; }
}

/// <summary>
/// 查詢虛擬資料夾列表請求。
/// </summary>
public class VirtualFolderListRequest
{
    public string RequestUserId { get; set; } = string.Empty;
    public FileScope Scope { get; set; } = FileScope.Personal;
    public string? ParentPath { get; set; }
}

/// <summary>
/// 檔案摘要資訊。
/// </summary>
public class FileEntryDto
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long SizeBytes { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
    public string[] Tags { get; set; } = [];
}

/// <summary>
/// chunk 上傳結果。
/// </summary>
public class ChunkUploadPartResult
{
    public string UploadId { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public bool Success { get; set; }
    public int UploadedChunks { get; set; }
}

/// <summary>
/// 下載結果。
/// </summary>
public class FileDownloadResult
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public long ContentLength { get; set; }
    public Stream Content { get; set; } = Stream.Null;
}

/// <summary>
/// 虛擬資料夾 DTO。
/// </summary>
public class VirtualFolderDto
{
    public string FolderId { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? ParentPath { get; set; }
    public int ChildrenCount { get; set; }
    public int FileCount { get; set; }
    public DateTimeOffset CreatedTime { get; set; }
    public DateTimeOffset UpdatedTime { get; set; }
}

/// <summary>
/// 檔案列表結果。
/// </summary>
public class FileListResult : PageListOutput<FileEntryDto>
{
}
