namespace Template.BusinessRule.RoleGroupService.Models;

/// <summary>
/// 使用者角色群組指派更新請求。
/// </summary>
public class UserRoleGroupUpdateRequest
{
    public string UserId { get; set; } = string.Empty;

    public List<int> RoleGroupIds { get; set; } = [];
}
