using System.ComponentModel;

namespace Template.BusinessRule.FunctionPermissionService.Enums;

/// <summary>
/// 功能操作權限服務常用訊息。
/// </summary>
public enum FunctionPermissionMessageEnum
{
    [Description("FunctionPermissionId 必須大於 0。")]
    FunctionPermissionIdMustBeGreaterThanZero = 1,

    [Description("ParentFunctionPermissionId 必須大於 0。")]
    ParentFunctionPermissionIdMustBeGreaterThanZero = 2,

    [Description("ParentFunctionPermissionId 不可等於 FunctionPermissionId。")]
    ParentFunctionPermissionIdCannotEqualFunctionPermissionId = 3,

    [Description("ParentFunctionPermissionId 不可設定為自己的子孫權限節點。")]
    ParentFunctionPermissionIdCannotBeDescendant = 4,

    [Description("上層功能操作權限不存在。")]
    ParentFunctionPermissionNotFound = 5,

    [Description("FunctionCode 為必填。")]
    FunctionCodeRequired = 6,

    [Description("FunctionName 為必填。")]
    FunctionNameRequired = 7,

    [Description("OperationCode 不是有效的功能操作代碼。")]
    OperationCodeInvalid = 8,

    [Description("PermissionKey 已存在。")]
    PermissionKeyAlreadyExists = 9,

    [Description("找不到功能操作權限資料。")]
    FunctionPermissionNotFound = 10,

    [Description("請提供正確的 FunctionPermissionId。")]
    InvalidFunctionPermissionId = 11,

    [Description("RoleGroupId 必須大於 0。")]
    RoleGroupIdMustBeGreaterThanZero = 12,

    [Description("找不到角色群組資料。")]
    RoleGroupNotFound = 13,

    [Description("一個或多個 FunctionPermissionIds 不存在。")]
    FunctionPermissionIdsNotFound = 14,

    [Description("FunctionPermissionIds 必須大於 0。")]
    FunctionPermissionIdsMustBeGreaterThanZero = 15,

    [Description("FunctionPermissionIds 不可重複。")]
    FunctionPermissionIdsDuplicate = 19,

    [Description("UserId 為必填。")]
    UserIdRequired = 16,

    [Description("更新成功。")]
    UpdatedSuccessfully = 17,

    [Description("刪除成功。")]
    DeletedSuccessfully = 18
}
