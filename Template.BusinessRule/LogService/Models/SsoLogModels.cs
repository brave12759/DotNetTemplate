namespace Template.BusinessRule.LogService.Models;

/// <summary>
/// 建立 SSO 日誌請求。
/// </summary>
public class SsoLogCreateRequest
{
    public DateTime? EventTime { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Metadata { get; set; }
}

/// <summary>
/// SSO 日誌查詢條件。
/// </summary>
public class SsoLogQueryRequest
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? OperatorId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// SSO 日誌輸出資料。
/// </summary>
public class SsoLogDto
{
    public long Id { get; set; }
    public DateTime EventTime { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
}

/// <summary>
/// SSO 日誌分頁結果。
/// </summary>
public class SsoLogQueryResult
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<SsoLogDto> Items { get; set; } = [];
}
