namespace Template.BusinessRule.FunctionPermissionService.Models;

/// <summary>
/// 功能操作權限更新請求。
/// </summary>
public class FunctionPermissionUpdateRequest
{
    public int FunctionPermissionId { get; set; }

    public int? ParentFunctionPermissionId { get; set; }

    public string FunctionCode { get; set; } = string.Empty;

    public string FunctionName { get; set; } = string.Empty;

    public string? OperationCode { get; set; }

    public int SortOrder { get; set; }

    public bool IsEnable { get; set; } = true;
}
