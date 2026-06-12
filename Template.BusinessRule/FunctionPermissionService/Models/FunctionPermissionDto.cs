namespace Template.BusinessRule.FunctionPermissionService.Models;

/// <summary>
/// 功能操作權限輸出模型。
/// </summary>
public class FunctionPermissionDto
{
    public int FunctionPermissionId { get; set; }

    public int? ParentFunctionPermissionId { get; set; }

    public string PermissionKey { get; set; } = string.Empty;

    public string FunctionCode { get; set; } = string.Empty;

    public string FunctionName { get; set; } = string.Empty;

    public string? OperationCode { get; set; }

    public string OperationName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsEnable { get; set; }

    public DateTime CreatedTime { get; set; }

    public string CreatedId { get; set; } = string.Empty;

    public DateTime UpdatedTime { get; set; }

    public string UpdatedId { get; set; } = string.Empty;

    public List<FunctionPermissionDto> Children { get; set; } = [];
}
