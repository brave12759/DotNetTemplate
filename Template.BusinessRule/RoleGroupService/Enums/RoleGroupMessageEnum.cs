using System.ComponentModel;

namespace Template.BusinessRule.RoleGroupService.Enums;

/// <summary>
/// 角色群組服務常用訊息。
/// </summary>
public enum RoleGroupMessageEnum
{
    [Description("RoleGroupId 必須大於 0。")]
    RoleGroupIdMustBeGreaterThanZero = 1,

    [Description("ParentRoleGroupId 必須大於 0。")]
    ParentRoleGroupIdMustBeGreaterThanZero = 2,

    [Description("ParentRoleGroupId 不可等於 RoleGroupId。")]
    ParentRoleGroupIdCannotEqualRoleGroupId = 3,

    [Description("ParentRoleGroupId 不可設定為自己的子孫角色群組。")]
    ParentRoleGroupIdCannotBeDescendant = 4,

    [Description("上層角色群組不存在。")]
    ParentRoleGroupNotFound = 5,

    [Description("RoleGroupName 為必填。")]
    RoleGroupNameRequired = 6,

    [Description("UserId 為必填。")]
    UserIdRequired = 7,

    [Description("RoleGroupIds 必須大於 0。")]
    RoleGroupIdsMustBeGreaterThanZero = 8,

    [Description("一個或多個 RoleGroupIds 不存在。")]
    RoleGroupIdsNotFound = 9,

    [Description("找不到角色群組資料。")]
    RoleGroupNotFound = 10,

    [Description("找不到使用者資料。")]
    UserNotFound = 11,

    [Description("請提供正確的 RoleGroupId。")]
    InvalidRoleGroupId = 12,

    [Description("更新成功。")]
    UpdatedSuccessfully = 13,

    [Description("刪除成功。")]
    DeletedSuccessfully = 14
}
