namespace Template.BusinessRule.DepartmentService.Models;

/// <summary>
/// 建立部門的請求模型。
/// </summary>
public class DepartmentCreateRequest
{
    /// <summary>
    /// 部門名稱。
    /// </summary>
    public string DeptName { get; set; } = string.Empty;

    /// <summary>
    /// 上層部門 ID；null 代表根部門。
    /// </summary>
    public int? ParentDeptId { get; set; }

    /// <summary>
    /// 同層部門排序值。
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否啟用。
    /// </summary>
    public bool IsEnable { get; set; } = true;
}
