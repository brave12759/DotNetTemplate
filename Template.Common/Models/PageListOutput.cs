namespace Template.Common.Models;

/// <summary>
/// 統一的清單查詢輸出格式，可同時支援分頁與不分頁回傳
/// </summary>
public class PageListOutput<T>
{
    /// <summary>
    /// 符合查詢條件的總筆數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 目前頁碼；未啟用分頁時固定為 1
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// 每頁筆數；未啟用分頁時代表本次回傳筆數
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 是否有啟用分頁
    /// </summary>
    public bool IsPaged { get; set; }

    /// <summary>
    /// 總頁數；未啟用分頁且有資料時為 1
    /// </summary>
    public int TotalPages
    {
        get
        {
            if (TotalCount == 0)
                return 0;

            if (!IsPaged)
                return 1;

            return (int)Math.Ceiling(TotalCount / (double)PageSize);
        }
    }

    /// <summary>
    /// 是否有上一頁
    /// </summary>
    public bool HasPreviousPage => IsPaged && Page > 1;

    /// <summary>
    /// 是否有下一頁
    /// </summary>
    public bool HasNextPage => IsPaged && Page < TotalPages;

    /// <summary>
    /// 本次查詢回傳的資料
    /// </summary>
    public IReadOnlyList<T> Items { get; set; } = [];
}
