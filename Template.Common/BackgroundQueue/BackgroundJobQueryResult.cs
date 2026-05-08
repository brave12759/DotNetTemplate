namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作分頁查詢結果。
/// </summary>
public class BackgroundJobQueryResult
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
    /// 本頁工作資料。
    /// </summary>
    public List<BackgroundJobDto> Items { get; set; } = [];
}
