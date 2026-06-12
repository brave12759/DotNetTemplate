namespace Template.BusinessRule.RoleGroupService.Models;

/// <summary>
/// 角色群組新增請求。
/// </summary>
public class RoleGroupCreateRequest
{
    public int? ParentRoleGroupId { get; set; }

    public string RoleGroupName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsEnable { get; set; } = true;
}
