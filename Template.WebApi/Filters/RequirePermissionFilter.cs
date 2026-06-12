using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.WebApi.Filters;

/// <summary>
/// 檢查目前使用者是否擁有 Action 所需的功能權限。
/// </summary>
public sealed class RequirePermissionFilter(
    string[] permissionKeys,
    ProjectDbContext db,
    ICurrentUserService currentUserService) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (permissionKeys.Length == 0)
            return;

        var userId = currentUserService.CurrentUser.UserId;
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (await UserHasPermissionAsync(userId, permissionKeys))
            return;

        context.Result = new ForbidResult();
    }

    /// <summary>
    /// 透過使用者角色群組與功能權限對應表，判斷使用者是否擁有任一必要權限鍵值。
    /// </summary>
    private async Task<bool> UserHasPermissionAsync(string userId, IReadOnlyCollection<string> requiredPermissionKeys)
    {
        return await (
            from userRoleGroup in db.Sys_UserRoleGroups.AsNoTracking()
            join rolePermission in db.Sys_RoleGroupFunctionPermissions.AsNoTracking()
                on userRoleGroup.RoleGroupId equals rolePermission.RoleGroupId
            join permission in db.Sys_FunctionPermissions.AsNoTracking()
                on rolePermission.FunctionPermissionId equals permission.FunctionPermissionId
            where userRoleGroup.UserId == userId &&
                  permission.IsEnable &&
                  requiredPermissionKeys.Contains(permission.PermissionKey)
            select permission.FunctionPermissionId)
            .AnyAsync();
    }
}
