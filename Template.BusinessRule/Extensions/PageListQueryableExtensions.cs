using Microsoft.EntityFrameworkCore;
using Template.Common.Models;

namespace Template.BusinessRule.Extensions;

/// <summary>
/// IQueryable 分頁工具，讓清單查詢可統一輸出 PageListOutput
/// </summary>
public static class PageListQueryableExtensions
{
    private const int MaxPageSize = 200;

    /// <summary>
    /// 將 IEnumerable 轉成統一清單輸出；適用於記憶體內集合
    /// </summary>
    public static PageListOutput<T> ToPageListOutput<T>(
        this IEnumerable<T> source,
        int page = 1,
        int pageSize = 50,
        bool enablePaging = false)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (enablePaging)
            ValidatePaging(page, pageSize);

        var items = source as IReadOnlyList<T> ?? source.ToList();
        var totalCount = items.Count;

        var pageItems = enablePaging
            ? items.Skip((page - 1) * pageSize).Take(pageSize).ToList()
            : (items as List<T> ?? items.ToList());

        return BuildPageListOutput(pageItems, totalCount, page, pageSize, enablePaging);
    }

    /// <summary>
    /// 將 IQueryable 轉成統一清單輸出；啟用分頁時會在資料庫端執行 Count、Skip、Take
    /// </summary>
    public static async Task<PageListOutput<T>> ToPageListOutputAsync<T>(
        this IQueryable<T> query,
        int page = 1,
        int pageSize = 50,
        bool enablePaging = false,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (!enablePaging)
        {
            var allItems = await query.ToListAsync(cancellationToken);
            return BuildPageListOutput(allItems, allItems.Count, page, pageSize, enablePaging: false);
        }

        ValidatePaging(page, pageSize);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return BuildPageListOutput(items, totalCount, page, pageSize, enablePaging: true);
    }

    private static PageListOutput<T> BuildPageListOutput<T>(
        IReadOnlyList<T> items,
        int totalCount,
        int page,
        int pageSize,
        bool enablePaging)
    {
        return new PageListOutput<T>
        {
            TotalCount = totalCount,
            Page = enablePaging ? page : 1,
            PageSize = enablePaging ? pageSize : items.Count,
            IsPaged = enablePaging,
            Items = items
        };
    }

    /// <summary>
    /// 檢查分頁參數，避免前端傳入無效頁碼或過大的每頁筆數。
    /// </summary>
    public static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1)
            throw new ArgumentException("Page 必須大於 0。", nameof(page));

        if (pageSize is < 1 or > MaxPageSize)
            throw new ArgumentException($"PageSize 必須介於 1 到 {MaxPageSize}。", nameof(pageSize));
    }
}
