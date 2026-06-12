namespace Template.Common.Settings;

/// <summary>
/// 檔案儲存核心設定。
/// 此設定只定義範本能力與限制，不綁定特定儲存供應商（S3/Azure Blob/NAS）。
/// </summary>
public class FileStorageSettings
{
    /// <summary>
    /// appsettings.json 區段名稱。
    /// </summary>
    public const string SectionName = "FileStorageSettings";

    /// <summary>
    /// 是否啟用檔案儲存核心能力。
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 儲存供應商識別字串，例如 Custom、Local、S3、AzureBlob。
    /// </summary>
    public string Provider { get; set; } = "Custom";

    /// <summary>
    /// 是否允許一般小檔一次上傳。
    /// </summary>
    public bool EnableSingleUpload { get; set; } = true;

    /// <summary>
    /// 是否允許大檔分塊（chunk）上傳。
    /// </summary>
    public bool EnableChunkUpload { get; set; } = true;

    /// <summary>
    /// 是否允許下載。
    /// </summary>
    public bool EnableDownload { get; set; } = true;

    /// <summary>
    /// 是否允許管理員檢視全部檔案（不受擁有者限制）。
    /// </summary>
    public bool EnableAdminScope { get; set; } = true;

    /// <summary>
    /// 是否允許個人範圍（只看自己的檔案）。
    /// </summary>
    public bool EnablePersonalScope { get; set; } = true;

    /// <summary>
    /// 是否啟用虛擬資料夾 CRUD。
    /// </summary>
    public bool EnableVirtualFolders { get; set; } = true;

    /// <summary>
    /// 單檔最大大小（MB）。
    /// </summary>
    public int MaxFileSizeMb { get; set; } = 5120;

    /// <summary>
    /// 單次上傳最大大小（MB），用於非 chunk 上傳。
    /// </summary>
    public int MaxSingleUploadSizeMb { get; set; } = 100;

    /// <summary>
    /// 單一 chunk 最大大小（MB）。
    /// </summary>
    public int MaxChunkSizeMb { get; set; } = 10;

    /// <summary>
    /// 單一檔案可上傳的最大 chunk 數量。
    /// </summary>
    public int MaxChunkCountPerFile { get; set; } = 1024;

    /// <summary>
    /// chunk 上傳會話逾時分鐘數。
    /// </summary>
    public int ChunkSessionExpireMinutes { get; set; } = 120;

    /// <summary>
    /// 下載連結有效秒數（供 presigned URL 或暫時授權下載）。
    /// </summary>
    public int DownloadUrlExpireSeconds { get; set; } = 300;

    /// <summary>
    /// 允許副檔名白名單（不含點），例如 pdf、jpg、zip。
    /// </summary>
    public string[] AllowedExtensions { get; set; } = [];

    /// <summary>
    /// 允許 MIME 類型白名單，例如 application/pdf。
    /// </summary>
    public string[] AllowedContentTypes { get; set; } = [];

    /// <summary>
    /// 提供商專用參數，例如 bucket/container/rootPath。
    /// </summary>
    public Dictionary<string, string> ProviderOptions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
