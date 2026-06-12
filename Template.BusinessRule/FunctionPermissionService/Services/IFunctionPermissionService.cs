using Template.BusinessRule.FunctionPermissionService.Models;
using Template.Common.Models;

namespace Template.BusinessRule.FunctionPermissionService.Services;

/// <summary>
/// 功能操作權限服務。
/// </summary>
public interface IFunctionPermissionService
{
    Task<PageListOutput<FunctionPermissionDto>> GetListAsync(
        string? keyword,
        bool? isEnable,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50);

    Task<IReadOnlyList<FunctionPermissionDto>> GetTreeAsync(bool? isEnable);

    Task<FunctionPermissionDto?> GetByIdAsync(int functionPermissionId);

    Task<FunctionPermissionDto> CreateAsync(FunctionPermissionCreateRequest request);

    Task<bool> UpdateAsync(FunctionPermissionUpdateRequest request);

    Task<bool> DeleteAsync(int functionPermissionId);

    Task<FunctionPermissionSyncResult> SyncFromMenuTreeAsync(bool includeDisabledMenus = false);

    Task<IReadOnlyList<FunctionPermissionDto>> GetRoleGroupPermissionsAsync(int roleGroupId, bool? isEnable);

    Task<IReadOnlyList<FunctionPermissionDto>> GetRoleGroupPermissionTreeAsync(int roleGroupId, bool? isEnable);

    Task<bool> UpdateRoleGroupPermissionsAsync(RoleGroupFunctionPermissionUpdateRequest request);

    Task<IReadOnlyList<FunctionPermissionDto>> GetUserPermissionTreeAsync(string userId, bool? isEnable);
}
