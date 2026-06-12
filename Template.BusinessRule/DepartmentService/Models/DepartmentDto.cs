namespace Template.BusinessRule.DepartmentService.Models;

/// <summary>
/// 部門輸出模型；樹狀查詢時會包含子部門集合。
/// </summary>
public class DepartmentDto
{
    /// <summary>
    /// 部門主鍵。
    /// </summary>
    public int DeptId { get; set; }

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
    public bool IsEnable { get; set; }

    /// <summary>
    /// 建立時間。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 建立者帳號。
    /// </summary>
    public string CreatedId { get; set; } = string.Empty;

    /// <summary>
    /// 最後更新時間。
    /// </summary>
    public DateTime UpdatedTime { get; set; }

    /// <summary>
    /// 最後更新者帳號。
    /// </summary>
    public string UpdatedId { get; set; } = string.Empty;

    /// <summary>
    /// 子部門集合。
    /// </summary>
    public List<DepartmentDto> Children { get; set; } = [];
}
