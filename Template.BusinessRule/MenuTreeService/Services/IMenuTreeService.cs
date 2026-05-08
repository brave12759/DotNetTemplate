using Template.BusinessRule.MenuTreeService.Models;

namespace Template.BusinessRule.MenuTreeService.Services;

/// <summary>
/// 選單樹服務介面。
/// </summary>
public interface IMenuTreeService
{
    /// <summary>
    /// 取得選單平面清單。
    /// </summary>
    Task<IReadOnlyList<MenuTreeDto>> GetListAsync(string? keyword, bool? isEnable);

    /// <summary>
    /// 取得父子階層選單樹。
    /// </summary>
    Task<IReadOnlyList<MenuTreeDto>> GetTreeAsync(bool? isEnable);

    /// <summary>
    /// 依主鍵取得選單。
    /// </summary>
    Task<MenuTreeDto?> GetByIdAsync(int id);

    /// <summary>
    /// 新增選單。
    /// </summary>
    Task<MenuTreeDto> CreateAsync(MenuTreeCreateRequest request);

    /// <summary>
    /// 更新選單。
    /// </summary>
    Task<bool> UpdateAsync(MenuTreeUpdateRequest request);

    /// <summary>
    /// 刪除選單。
    /// </summary>
    Task<bool> DeleteAsync(int id);
}
