using Template.BusinessRule.DepartmentService.Models;
using Template.Common.Models;

namespace Template.BusinessRule.DepartmentService.Services;

/// <summary>
/// 部門維護服務介面。
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// 取得部門清單，可依關鍵字與啟用狀態篩選。
    /// </summary>
    Task<PageListOutput<DepartmentDto>> GetListAsync(
        string? keyword,
        bool? isEnable,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// 取得部門樹狀清單。
    /// </summary>
    Task<IReadOnlyList<DepartmentDto>> GetTreeAsync(bool? isEnable);

    /// <summary>
    /// 依部門 ID 取得單筆部門。
    /// </summary>
    Task<DepartmentDto?> GetByIdAsync(int deptId);

    /// <summary>
    /// 建立部門。
    /// </summary>
    Task<DepartmentDto> CreateAsync(DepartmentCreateRequest request);

    /// <summary>
    /// 更新部門。
    /// </summary>
    Task<bool> UpdateAsync(DepartmentUpdateRequest request);

    /// <summary>
    /// 刪除部門；有子部門或使用者歸屬時不可刪除。
    /// </summary>
    Task<bool> DeleteAsync(int deptId);
}
