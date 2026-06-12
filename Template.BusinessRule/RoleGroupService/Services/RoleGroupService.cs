using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.BusinessRule.Extensions;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.RoleGroupService.Enums;
using Template.BusinessRule.RoleGroupService.Models;
using Template.Common.Enums;
using Template.Common.Extensions;
using Template.Common.Models;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.RoleGroupService.Services;

/// <summary>
/// 角色群組 CRUD、階層樹查詢與使用者角色群組指派服務。
/// </summary>
public class RoleGroupService(IServiceProvider serviceProvider) : BaseService(serviceProvider), IRoleGroupService
{
    private const int MaxPageSize = 200;

    private readonly Lazy<ILogService?> _logService = new(() => serviceProvider.GetService<ILogService>());

    /// <inheritdoc />
    public async Task<PageListOutput<RoleGroupDto>> GetListAsync(
        string? keyword,
        bool? isEnable,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50)
    {
        if (enablePaging)
            PageListQueryableExtensions.ValidatePaging(page, pageSize);

        var query = Db.Sys_RoleGroups.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(r =>
                r.RoleGroupName.Contains(k) ||
                r.Description.Contains(k));
        }

        if (isEnable.HasValue)
            query = query.Where(r => r.IsEnable == isEnable.Value);

        return await query
            .OrderBy(r => r.ParentRoleGroupId)
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.RoleGroupId)
            .Select(ToDtoExpression())
            .ToPageListOutputAsync(page, pageSize, enablePaging);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleGroupDto>> GetTreeAsync(bool? isEnable)
    {
        var query = Db.Sys_RoleGroups.AsNoTracking().AsQueryable();

        if (isEnable.HasValue)
            query = query.Where(r => r.IsEnable == isEnable.Value);

        var roleGroups = await query
            .OrderBy(r => r.ParentRoleGroupId)
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.RoleGroupId)
            .Select(ToDtoExpression())
            .ToListAsync();

        return TreeBuilder.BuildTree(
            roleGroups,
            r => r.RoleGroupId,
            r => r.ParentRoleGroupId,
            CloneWithoutChildren,
            r => r.Children,
            CompareRoleGroups);
    }

    /// <inheritdoc />
    public async Task<RoleGroupDto?> GetByIdAsync(int roleGroupId)
    {
        if (roleGroupId <= 0)
            throw new ArgumentException(Message(RoleGroupMessageEnum.RoleGroupIdMustBeGreaterThanZero), nameof(roleGroupId));

        return await Db.Sys_RoleGroups
            .AsNoTracking()
            .Where(r => r.RoleGroupId == roleGroupId)
            .Select(ToDtoExpression())
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<RoleGroupDto> CreateAsync(RoleGroupCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request.RoleGroupName);

        await EnsureParentExistsAsync(request.ParentRoleGroupId);

        var now = DateTime.UtcNow;
        var entity = new Sys_RoleGroup
        {
            ParentRoleGroupId = request.ParentRoleGroupId,
            RoleGroupName = request.RoleGroupName.Trim(),
            Description = NormalizeOptionalText(request.Description),
            SortOrder = request.SortOrder,
            IsEnable = request.IsEnable,
            CreatedTime = now,
            CreatedId = CurrentUser.UserId,
            UpdatedTime = now,
            UpdatedId = CurrentUser.UserId
        };

        Db.Sys_RoleGroups.Add(entity);
        await Db.SaveChangesAsync();
        await WriteRoleGroupLogAsync(
            AuditActionEnum.Create,
            entity.RoleGroupId,
            "建立角色群組。",
            newValue: MapToDto(entity));

        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(RoleGroupUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RoleGroupId <= 0)
            throw new ArgumentException(Message(RoleGroupMessageEnum.RoleGroupIdMustBeGreaterThanZero), nameof(request.RoleGroupId));

        ValidateRequest(request.RoleGroupName);

        if (request.ParentRoleGroupId == request.RoleGroupId)
            throw new ArgumentException(Message(RoleGroupMessageEnum.ParentRoleGroupIdCannotEqualRoleGroupId), nameof(request.ParentRoleGroupId));

        var entity = await Db.Sys_RoleGroups.FirstOrDefaultAsync(r => r.RoleGroupId == request.RoleGroupId);
        if (entity is null)
            return false;

        var oldValue = MapToDto(entity);
        await EnsureParentExistsAsync(request.ParentRoleGroupId);
        await EnsureParentIsNotDescendantAsync(request.RoleGroupId, request.ParentRoleGroupId);

        entity.ParentRoleGroupId = request.ParentRoleGroupId;
        entity.RoleGroupName = request.RoleGroupName.Trim();
        entity.Description = NormalizeOptionalText(request.Description);
        entity.SortOrder = request.SortOrder;
        entity.IsEnable = request.IsEnable;
        entity.UpdatedTime = DateTime.UtcNow;
        entity.UpdatedId = CurrentUser.UserId;

        await Db.SaveChangesAsync();
        await WriteRoleGroupLogAsync(
            AuditActionEnum.Update,
            entity.RoleGroupId,
            "更新角色群組。",
            oldValue,
            MapToDto(entity));
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int roleGroupId)
    {
        if (roleGroupId <= 0)
            throw new ArgumentException(Message(RoleGroupMessageEnum.RoleGroupIdMustBeGreaterThanZero), nameof(roleGroupId));

        var exists = await Db.Sys_RoleGroups.AnyAsync(r => r.RoleGroupId == roleGroupId);
        if (!exists)
            return false;

        var descendantIds = await GetDescendantIdsAsync(roleGroupId);
        var deleteIds = descendantIds.Append(roleGroupId).ToList();

        var mappings = await Db.Sys_UserRoleGroups
            .Where(r => deleteIds.Contains(r.RoleGroupId))
            .ToListAsync();
        Db.Sys_UserRoleGroups.RemoveRange(mappings);

        var roleGroups = await Db.Sys_RoleGroups
            .Where(r => deleteIds.Contains(r.RoleGroupId))
            .ToListAsync();
        var oldValue = roleGroups.Select(MapToDto).ToList();
        Db.Sys_RoleGroups.RemoveRange(roleGroups);

        await Db.SaveChangesAsync();
        await WriteRoleGroupLogAsync(
            AuditActionEnum.Delete,
            roleGroupId,
            "刪除角色群組與其子角色群組。",
            oldValue: oldValue,
            metadata: new { DeleteIds = deleteIds });
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleGroupDto>> GetUserRoleGroupsAsync(string userId, bool? isEnable)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(Message(RoleGroupMessageEnum.UserIdRequired), nameof(userId));

        var normalizedUserId = userId.Trim();
        var query =
            from mapping in Db.Sys_UserRoleGroups.AsNoTracking()
            join roleGroup in Db.Sys_RoleGroups.AsNoTracking()
                on mapping.RoleGroupId equals roleGroup.RoleGroupId
            where mapping.UserId == normalizedUserId
            select roleGroup;

        if (isEnable.HasValue)
            query = query.Where(r => r.IsEnable == isEnable.Value);

        return await query
            .OrderBy(r => r.ParentRoleGroupId)
            .ThenBy(r => r.SortOrder)
            .ThenBy(r => r.RoleGroupId)
            .Select(ToDtoExpression())
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> UpdateUserRoleGroupsAsync(UserRoleGroupUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException(Message(RoleGroupMessageEnum.UserIdRequired), nameof(request.UserId));

        var userId = request.UserId.Trim();
        var userExists = await Db.Sys_UserInfos.AnyAsync(u => u.UserId == userId);
        if (!userExists)
            return false;

        var requestRoleGroupIds = request.RoleGroupIds ?? [];
        var roleGroupIds = requestRoleGroupIds
            .Where(id => id > 0)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (roleGroupIds.Count != requestRoleGroupIds.Distinct().Count())
            throw new ArgumentException(Message(RoleGroupMessageEnum.RoleGroupIdsMustBeGreaterThanZero), nameof(request.RoleGroupIds));

        var existingRoleGroupIds = await Db.Sys_RoleGroups
            .Where(r => roleGroupIds.Contains(r.RoleGroupId))
            .Select(r => r.RoleGroupId)
            .ToListAsync();

        if (existingRoleGroupIds.Count != roleGroupIds.Count)
            throw new ArgumentException(Message(RoleGroupMessageEnum.RoleGroupIdsNotFound), nameof(request.RoleGroupIds));

        var currentMappings = await Db.Sys_UserRoleGroups
            .Where(r => r.UserId == userId)
            .ToListAsync();

        Db.Sys_UserRoleGroups.RemoveRange(currentMappings);

        var now = DateTime.UtcNow;
        foreach (var roleGroupId in roleGroupIds)
        {
            Db.Sys_UserRoleGroups.Add(new Sys_UserRoleGroup
            {
                UserId = userId,
                RoleGroupId = roleGroupId,
                CreatedTime = now,
                CreatedId = CurrentUser.UserId
            });
        }

        await Db.SaveChangesAsync();
        await WriteRoleGroupLogAsync(
            AuditActionEnum.PermissionChange,
            0,
            "更新使用者角色群組。",
            oldValue: new { UserId = userId, RoleGroupIds = currentMappings.Select(x => x.RoleGroupId).OrderBy(x => x).ToList() },
            newValue: new { UserId = userId, RoleGroupIds = roleGroupIds },
            metadata: new { TargetUserId = userId });
        return true;
    }

    /// <summary>
    /// 寫入角色群組操作日誌；測試未註冊 LogService 時略過。
    /// </summary>
    private Task WriteRoleGroupLogAsync(
        AuditActionEnum action,
        int roleGroupId,
        string message,
        object? oldValue = null,
        object? newValue = null,
        object? metadata = null)
    {
        return _logService.Value?.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "RoleGroup",
            Action = action,
            Result = AuditResultEnum.Success,
            TargetType = nameof(Sys_RoleGroup),
            TargetId = roleGroupId > 0 ? roleGroupId.ToString() : string.Empty,
            Message = message,
            OldValue = oldValue,
            NewValue = newValue,
            Metadata = metadata
        }) ?? Task.CompletedTask;
    }

    /// <summary>
    /// 檢查上層角色群組是否為有效既有群組；空值代表根群組。
    /// </summary>
    private async Task EnsureParentExistsAsync(int? parentRoleGroupId)
    {
        if (!parentRoleGroupId.HasValue)
            return;

        if (parentRoleGroupId.Value <= 0)
            throw new ArgumentException(Message(RoleGroupMessageEnum.ParentRoleGroupIdMustBeGreaterThanZero), nameof(parentRoleGroupId));

        var exists = await Db.Sys_RoleGroups.AnyAsync(r => r.RoleGroupId == parentRoleGroupId.Value);
        if (!exists)
            throw new ArgumentException(Message(RoleGroupMessageEnum.ParentRoleGroupNotFound), nameof(parentRoleGroupId));
    }

    /// <summary>
    /// 檢查更新上層角色群組時不會移到自己的後代底下，避免樹狀循環。
    /// </summary>
    private async Task EnsureParentIsNotDescendantAsync(int roleGroupId, int? parentRoleGroupId)
    {
        if (!parentRoleGroupId.HasValue)
            return;

        var roleGroups = await Db.Sys_RoleGroups
            .AsNoTracking()
            .Select(r => new { r.RoleGroupId, r.ParentRoleGroupId })
            .ToListAsync();

        var currentParentRoleGroupId = parentRoleGroupId.Value;
        while (true)
        {
            if (currentParentRoleGroupId == roleGroupId)
                throw new ArgumentException(Message(RoleGroupMessageEnum.ParentRoleGroupIdCannotBeDescendant), nameof(parentRoleGroupId));

            var parent = roleGroups.FirstOrDefault(r => r.RoleGroupId == currentParentRoleGroupId);
            if (parent?.ParentRoleGroupId is null)
                return;

            currentParentRoleGroupId = parent.ParentRoleGroupId.Value;
        }
    }

    /// <summary>
    /// 取得指定角色群組底下所有後代群組 ID。
    /// </summary>
    private async Task<List<int>> GetDescendantIdsAsync(int roleGroupId)
    {
        var roleGroups = await Db.Sys_RoleGroups
            .AsNoTracking()
            .Select(r => new { r.RoleGroupId, r.ParentRoleGroupId })
            .ToListAsync();

        var descendants = new List<int>();
        var pending = new Queue<int>();
        pending.Enqueue(roleGroupId);

        while (pending.Count > 0)
        {
            var parentId = pending.Dequeue();
            foreach (var child in roleGroups.Where(r => r.ParentRoleGroupId == parentId))
            {
                descendants.Add(child.RoleGroupId);
                pending.Enqueue(child.RoleGroupId);
            }
        }

        return descendants;
    }

    /// <summary>
    /// 驗證角色群組請求必要欄位。
    /// </summary>
    private static void ValidateRequest(string roleGroupName)
    {
        if (string.IsNullOrWhiteSpace(roleGroupName))
            throw new ArgumentException(Message(RoleGroupMessageEnum.RoleGroupNameRequired), nameof(roleGroupName));
    }

    private static string NormalizeOptionalText(string? value) => value?.Trim() ?? string.Empty;

    /// <summary>
    /// 取得角色群組訊息列舉的描述文字。
    /// </summary>
    private static string Message(RoleGroupMessageEnum message) => message.GetDescription();

    private static int CompareRoleGroups(RoleGroupDto left, RoleGroupDto right)
    {
        var sortOrder = left.SortOrder.CompareTo(right.SortOrder);
        return sortOrder != 0 ? sortOrder : left.RoleGroupId.CompareTo(right.RoleGroupId);
    }

    /// <summary>
    /// 複製角色群組 DTO 並清空 Children，避免組樹時重用原集合。
    /// </summary>
    private static RoleGroupDto CloneWithoutChildren(RoleGroupDto roleGroup)
    {
        return new RoleGroupDto
        {
            RoleGroupId = roleGroup.RoleGroupId,
            ParentRoleGroupId = roleGroup.ParentRoleGroupId,
            RoleGroupName = roleGroup.RoleGroupName,
            Description = roleGroup.Description,
            SortOrder = roleGroup.SortOrder,
            IsEnable = roleGroup.IsEnable,
            CreatedTime = roleGroup.CreatedTime,
            CreatedId = roleGroup.CreatedId,
            UpdatedTime = roleGroup.UpdatedTime,
            UpdatedId = roleGroup.UpdatedId
        };
    }

    /// <summary>
    /// 將角色群組資料表實體轉成輸出 DTO。
    /// </summary>
    private static RoleGroupDto MapToDto(Sys_RoleGroup roleGroup)
    {
        return new RoleGroupDto
        {
            RoleGroupId = roleGroup.RoleGroupId,
            ParentRoleGroupId = roleGroup.ParentRoleGroupId,
            RoleGroupName = roleGroup.RoleGroupName,
            Description = roleGroup.Description,
            SortOrder = roleGroup.SortOrder,
            IsEnable = roleGroup.IsEnable,
            CreatedTime = roleGroup.CreatedTime,
            CreatedId = roleGroup.CreatedId,
            UpdatedTime = roleGroup.UpdatedTime,
            UpdatedId = roleGroup.UpdatedId
        };
    }

    /// <summary>
    /// 建立 EF Core 可轉譯的角色群組實體到 DTO 投影運算式。
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<Sys_RoleGroup, RoleGroupDto>> ToDtoExpression()
    {
        return roleGroup => new RoleGroupDto
        {
            RoleGroupId = roleGroup.RoleGroupId,
            ParentRoleGroupId = roleGroup.ParentRoleGroupId,
            RoleGroupName = roleGroup.RoleGroupName,
            Description = roleGroup.Description,
            SortOrder = roleGroup.SortOrder,
            IsEnable = roleGroup.IsEnable,
            CreatedTime = roleGroup.CreatedTime,
            CreatedId = roleGroup.CreatedId,
            UpdatedTime = roleGroup.UpdatedTime,
            UpdatedId = roleGroup.UpdatedId
        };
    }
}
