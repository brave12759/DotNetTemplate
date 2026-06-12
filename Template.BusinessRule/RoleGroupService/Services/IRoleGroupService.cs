using Template.BusinessRule.RoleGroupService.Models;
using Template.Common.Models;

namespace Template.BusinessRule.RoleGroupService.Services;

/// <summary>
/// 角色群組服務。
/// </summary>
public interface IRoleGroupService
{
    /// <summary>
    /// 取得角色群組平面清單。
    /// </summary>
    Task<PageListOutput<RoleGroupDto>> GetListAsync(
        string? keyword,
        bool? isEnable,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// 取得角色群組父子階層樹。
    /// </summary>
    Task<IReadOnlyList<RoleGroupDto>> GetTreeAsync(bool? isEnable);

    /// <summary>
    /// 依 RoleGroupId 取得單筆角色群組。
    /// </summary>
    Task<RoleGroupDto?> GetByIdAsync(int roleGroupId);

    /// <summary>
    /// 新增角色群組。
    /// </summary>
    Task<RoleGroupDto> CreateAsync(RoleGroupCreateRequest request);

    /// <summary>
    /// 更新角色群組。
    /// </summary>
    Task<bool> UpdateAsync(RoleGroupUpdateRequest request);

    /// <summary>
    /// 刪除角色群組；會同步刪除所有子孫角色群組與使用者對應資料。
    /// </summary>
    Task<bool> DeleteAsync(int roleGroupId);

    /// <summary>
    /// 取得指定使用者擁有的角色群組。
    /// </summary>
    Task<IReadOnlyList<RoleGroupDto>> GetUserRoleGroupsAsync(string userId, bool? isEnable);

    /// <summary>
    /// 以整批覆蓋方式更新指定使用者擁有的角色群組。
    /// </summary>
    Task<bool> UpdateUserRoleGroupsAsync(UserRoleGroupUpdateRequest request);
}
