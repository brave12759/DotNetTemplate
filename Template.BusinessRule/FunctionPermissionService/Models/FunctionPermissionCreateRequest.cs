namespace Template.BusinessRule.FunctionPermissionService.Models;

/// <summary>
/// 功能操作權限新增請求。
/// </summary>
public class FunctionPermissionCreateRequest
{
    public int? ParentFunctionPermissionId { get; set; }

    public string FunctionCode { get; set; } = string.Empty;

    public string FunctionName { get; set; } = string.Empty;

    public string? OperationCode { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnable { get; set; } = true;
}
