namespace Template.BusinessRule.FunctionPermissionService.Models;

/// <summary>
/// 功能操作權限同步結果。
/// </summary>
public class FunctionPermissionSyncResult
{
    public int CreatedFunctionCount { get; set; }

    public int CreatedOperationCount { get; set; }

    public int ExistingFunctionCount { get; set; }

    public int ExistingOperationCount { get; set; }
}
