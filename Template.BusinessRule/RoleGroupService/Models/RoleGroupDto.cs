namespace Template.BusinessRule.RoleGroupService.Models;

/// <summary>
/// 角色群組輸出模型。
/// </summary>
public class RoleGroupDto
{
    public int RoleGroupId { get; set; }

    public int? ParentRoleGroupId { get; set; }

    public string RoleGroupName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsEnable { get; set; }

    public DateTime CreatedTime { get; set; }

    public string CreatedId { get; set; } = string.Empty;

    public DateTime UpdatedTime { get; set; }

    public string UpdatedId { get; set; } = string.Empty;

    public List<RoleGroupDto> Children { get; set; } = [];
}
