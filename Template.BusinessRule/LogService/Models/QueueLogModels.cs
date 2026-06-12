namespace Template.BusinessRule.LogService.Models;

/// <summary>
/// 建立佇列日誌請求。
/// </summary>
public class QueueLogCreateRequest
{
    public DateTime? EventTime { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public long JobId { get; set; }
    public int WorkType { get; set; }
    public string WorkKey { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public int Status { get; set; }
    public int RetryCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public object? Metadata { get; set; }
}

/// <summary>
/// 佇列日誌查詢條件。
/// </summary>
public class QueueLogQueryRequest
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? OperatorId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// 佇列日誌輸出資料。
/// </summary>
public class QueueLogDto
{
    public long Id { get; set; }
    public DateTime EventTime { get; set; }
    public string OperatorId { get; set; } = string.Empty;
    public long JobId { get; set; }
    public int WorkType { get; set; }
    public string WorkKey { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public int Status { get; set; }
    public int RetryCount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string MetadataJson { get; set; } = string.Empty;
}

/// <summary>
/// 佇列日誌分頁結果。
/// </summary>
public class QueueLogQueryResult
{
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public IReadOnlyList<QueueLogDto> Items { get; set; } = [];
}
