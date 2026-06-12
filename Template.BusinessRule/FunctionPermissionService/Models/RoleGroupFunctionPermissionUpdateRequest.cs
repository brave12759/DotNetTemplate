namespace Template.BusinessRule.FunctionPermissionService.Models;

/// <summary>
/// 角色群組功能操作權限指派更新請求。
/// </summary>
public class RoleGroupFunctionPermissionUpdateRequest
{
    public int RoleGroupId { get; set; }

    public List<int> FunctionPermissionIds { get; set; } = [];
}
