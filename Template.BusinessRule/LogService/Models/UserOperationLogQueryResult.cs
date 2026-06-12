namespace Template.BusinessRule.LogService.Models;

/// <summary>
/// 使用者操作日誌分頁查詢結果。
/// </summary>
public class UserOperationLogQueryResult
{
    /// <summary>
    /// 符合條件的總筆數。
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 目前頁碼。
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// 每頁筆數。
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 本頁使用者操作日誌清單。
    /// </summary>
    public IReadOnlyList<UserOperationLogDto> Items { get; set; } = [];
}
