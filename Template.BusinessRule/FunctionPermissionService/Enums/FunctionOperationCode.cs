using System.ComponentModel;

namespace Template.BusinessRule.FunctionPermissionService.Enums;

/// <summary>
/// 功能操作代碼。
/// </summary>
public enum FunctionOperationCode
{
    [Description("新增")]
    C = 1,

    [Description("讀取")]
    R = 2,

    [Description("更新")]
    U = 3,

    [Description("刪除")]
    D = 4,

    [Description("審核")]
    A = 5,

    [Description("檔案上傳/下載")]
    F = 6
}
