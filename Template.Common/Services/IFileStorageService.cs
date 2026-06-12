using Template.Common.FileStorage;

namespace Template.Common.Services;

/// <summary>
/// 檔案儲存核心服務介面
/// 由各專案自行實作，範本只提供契約與設定
/// </summary>
public interface IFileStorageService
{
    Task<FileEntryDto> UploadSingleAsync(SingleFileUploadRequest request, CancellationToken cancellationToken = default);
    Task<ChunkUploadSession> InitializeChunkUploadAsync(ChunkUploadInitRequest request, CancellationToken cancellationToken = default);
    Task<ChunkUploadPartResult> UploadChunkAsync(ChunkUploadPartRequest request, CancellationToken cancellationToken = default);
    Task<FileEntryDto> CompleteChunkUploadAsync(ChunkUploadCompleteRequest request, CancellationToken cancellationToken = default);

    Task<FileDownloadResult> DownloadAsync(FileDownloadRequest request, CancellationToken cancellationToken = default);

    Task<FileListResult> ListFilesAsync(FileListQueryRequest request, CancellationToken cancellationToken = default);
    Task<FileEntryDto?> GetFileAsync(string fileId, string requestUserId, FileScope scope, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(FileDeleteRequest request, CancellationToken cancellationToken = default);

    Task<VirtualFolderDto> CreateFolderAsync(VirtualFolderCreateRequest request, CancellationToken cancellationToken = default);
    Task<VirtualFolderDto> UpdateFolderAsync(VirtualFolderUpdateRequest request, CancellationToken cancellationToken = default);
    Task DeleteFolderAsync(VirtualFolderDeleteRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VirtualFolderDto>> ListFoldersAsync(VirtualFolderListRequest request, CancellationToken cancellationToken = default);
}
