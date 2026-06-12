using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.BusinessRule.Extensions;
using Template.BusinessRule.FunctionPermissionService.Enums;
using Template.BusinessRule.FunctionPermissionService.Models;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.Common.Enums;
using Template.Common.Extensions;
using Template.Common.Models;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.FunctionPermissionService.Services;

/// <summary>
/// 功能操作權限 CRUD、階層樹查詢、同步補足與角色群組指派服務。
/// </summary>
public class FunctionPermissionService(IServiceProvider serviceProvider) : BaseService(serviceProvider), IFunctionPermissionService
{
    private readonly Lazy<ILogService?> _logService = new(() => serviceProvider.GetService<ILogService>());

    /// <inheritdoc />
    public async Task<PageListOutput<FunctionPermissionDto>> GetListAsync(
        string? keyword,
        bool? isEnable,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50)
    {
        if (enablePaging)
            PageListQueryableExtensions.ValidatePaging(page, pageSize);

        var query = Db.Sys_FunctionPermissions.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(p =>
                p.PermissionKey.Contains(k) ||
                p.FunctionCode.Contains(k) ||
                p.FunctionName.Contains(k) ||
                p.OperationName.Contains(k));
        }

        if (isEnable.HasValue)
            query = query.Where(p => p.IsEnable == isEnable.Value);

        return await query
            .OrderBy(p => p.ParentFunctionPermissionId)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.FunctionPermissionId)
            .Select(ToDtoExpression())
            .ToPageListOutputAsync(page, pageSize, enablePaging);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FunctionPermissionDto>> GetTreeAsync(bool? isEnable)
    {
        var query = Db.Sys_FunctionPermissions.AsNoTracking().AsQueryable();

        if (isEnable.HasValue)
            query = query.Where(p => p.IsEnable == isEnable.Value);

        var permissions = await query
            .OrderBy(p => p.ParentFunctionPermissionId)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.FunctionPermissionId)
            .Select(ToDtoExpression())
            .ToListAsync();

        return TreeBuilder.BuildTree(
            permissions,
            p => p.FunctionPermissionId,
            p => p.ParentFunctionPermissionId,
            CloneWithoutChildren,
            p => p.Children,
            ComparePermissions);
    }

    /// <inheritdoc />
    public async Task<FunctionPermissionDto?> GetByIdAsync(int functionPermissionId)
    {
        if (functionPermissionId <= 0)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.FunctionPermissionIdMustBeGreaterThanZero), nameof(functionPermissionId));

        return await Db.Sys_FunctionPermissions
            .AsNoTracking()
            .Where(p => p.FunctionPermissionId == functionPermissionId)
            .Select(ToDtoExpression())
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<FunctionPermissionDto> CreateAsync(FunctionPermissionCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request.FunctionCode, request.FunctionName, request.OperationCode);
        await EnsureParentExistsAsync(request.ParentFunctionPermissionId);

        var operationCode = NormalizeOperationCode(request.OperationCode);
        var permissionKey = BuildPermissionKey(request.FunctionCode, operationCode);
        var exists = await Db.Sys_FunctionPermissions.AnyAsync(p => p.PermissionKey == permissionKey);
        if (exists)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.PermissionKeyAlreadyExists), nameof(request.FunctionCode));

        var now = DateTime.UtcNow;
        var entity = new Sys_FunctionPermission
        {
            ParentFunctionPermissionId = request.ParentFunctionPermissionId,
            PermissionKey = permissionKey,
            FunctionCode = request.FunctionCode.Trim(),
            FunctionName = request.FunctionName.Trim(),
            OperationCode = operationCode,
            OperationName = GetOperationName(operationCode),
            SortOrder = request.SortOrder,
            IsEnable = request.IsEnable,
            CreatedTime = now,
            CreatedId = CurrentUser.UserId,
            UpdatedTime = now,
            UpdatedId = CurrentUser.UserId
        };

        Db.Sys_FunctionPermissions.Add(entity);
        await Db.SaveChangesAsync();

        await WriteFunctionPermissionLogAsync(
            AuditActionEnum.Create,
            entity.FunctionPermissionId,
            "建立功能操作權限。",
            newValue: MapToDto(entity));

        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(FunctionPermissionUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.FunctionPermissionId <= 0)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.FunctionPermissionIdMustBeGreaterThanZero), nameof(request.FunctionPermissionId));

        ValidateRequest(request.FunctionCode, request.FunctionName, request.OperationCode);

        if (request.ParentFunctionPermissionId == request.FunctionPermissionId)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.ParentFunctionPermissionIdCannotEqualFunctionPermissionId), nameof(request.ParentFunctionPermissionId));

        var entity = await Db.Sys_FunctionPermissions.FirstOrDefaultAsync(p => p.FunctionPermissionId == request.FunctionPermissionId);
        if (entity is null)
            return false;

        var oldValue = MapToDto(entity);

        await EnsureParentExistsAsync(request.ParentFunctionPermissionId);
        await EnsureParentIsNotDescendantAsync(request.FunctionPermissionId, request.ParentFunctionPermissionId);

        var operationCode = NormalizeOperationCode(request.OperationCode);
        var permissionKey = BuildPermissionKey(request.FunctionCode, operationCode);
        var keyExists = await Db.Sys_FunctionPermissions.AnyAsync(p =>
            p.FunctionPermissionId != request.FunctionPermissionId &&
            p.PermissionKey == permissionKey);
        if (keyExists)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.PermissionKeyAlreadyExists), nameof(request.FunctionCode));

        entity.ParentFunctionPermissionId = request.ParentFunctionPermissionId;
        entity.PermissionKey = permissionKey;
        entity.FunctionCode = request.FunctionCode.Trim();
        entity.FunctionName = request.FunctionName.Trim();
        entity.OperationCode = operationCode;
        entity.OperationName = GetOperationName(operationCode);
        entity.SortOrder = request.SortOrder;
        entity.IsEnable = request.IsEnable;
        entity.UpdatedTime = DateTime.UtcNow;
        entity.UpdatedId = CurrentUser.UserId;

        await Db.SaveChangesAsync();

        await WriteFunctionPermissionLogAsync(
            AuditActionEnum.Update,
            entity.FunctionPermissionId,
            "更新功能操作權限。",
            oldValue,
            MapToDto(entity));

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int functionPermissionId)
    {
        if (functionPermissionId <= 0)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.FunctionPermissionIdMustBeGreaterThanZero), nameof(functionPermissionId));

        var exists = await Db.Sys_FunctionPermissions.AnyAsync(p => p.FunctionPermissionId == functionPermissionId);
        if (!exists)
            return false;

        var descendantIds = await GetDescendantIdsAsync(functionPermissionId);
        var deleteIds = descendantIds.Append(functionPermissionId).ToList();

        var mappings = await Db.Sys_RoleGroupFunctionPermissions
            .Where(p => deleteIds.Contains(p.FunctionPermissionId))
            .ToListAsync();
        Db.Sys_RoleGroupFunctionPermissions.RemoveRange(mappings);

        var permissions = await Db.Sys_FunctionPermissions
            .Where(p => deleteIds.Contains(p.FunctionPermissionId))
            .ToListAsync();

        var deletedSnapshots = permissions
            .Select(MapToDto)
            .ToList();

        Db.Sys_FunctionPermissions.RemoveRange(permissions);

        await Db.SaveChangesAsync();

        await WriteFunctionPermissionLogAsync(
            AuditActionEnum.Delete,
            functionPermissionId,
            "刪除功能操作權限及其子孫節點。",
            oldValue: deletedSnapshots,
            metadata: new
            {
                DeletedPermissionCount = deletedSnapshots.Count,
                DeletedRoleGroupMappingCount = mappings.Count
            });

        return true;
    }

    /// <inheritdoc />
    public async Task<FunctionPermissionSyncResult> SyncFromMenuTreeAsync(bool includeDisabledMenus = false)
    {
        var menus = await Db.Sys_MenuTrees
            .AsNoTracking()
            .Where(m => includeDisabledMenus || m.IsEnable)
            .OrderBy(m => m.ParentId)
            .ThenBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .ToListAsync();

        var result = new FunctionPermissionSyncResult();
        if (menus.Count == 0)
            return result;

        await using var transaction = Db.Database.IsRelational()
            ? await Db.Database.BeginTransactionAsync()
            : null;

        var now = DateTime.UtcNow;
        var operations = GetOperationDefinitions();
        var menuCodes = menus
            .Select(m => m.MenuCode.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct()
            .ToList();
        var permissionKeys = menuCodes
            .Concat(menuCodes.SelectMany(code => operations.Select(operation => BuildPermissionKey(code, operation.Code))))
            .Distinct()
            .ToList();
        var existingPermissions = await Db.Sys_FunctionPermissions
            .Where(p => permissionKeys.Contains(p.PermissionKey))
            .ToListAsync();
        var permissionLookup = existingPermissions.ToDictionary(p => p.PermissionKey);
        var functionPermissionsByMenuId = new Dictionary<int, Sys_FunctionPermission>();

        foreach (var menu in menus.Where(m => !string.IsNullOrWhiteSpace(m.MenuCode)))
        {
            var functionKey = menu.MenuCode.Trim();
            if (!permissionLookup.TryGetValue(functionKey, out var functionPermission))
            {
                functionPermission = new Sys_FunctionPermission
                {
                    PermissionKey = functionKey,
                    FunctionCode = functionKey,
                    FunctionName = menu.MenuName.Trim(),
                    OperationCode = null,
                    OperationName = string.Empty,
                    SortOrder = menu.SortOrder,
                    IsEnable = menu.IsEnable,
                    CreatedTime = now,
                    CreatedId = CurrentUser.UserId,
                    UpdatedTime = now,
                    UpdatedId = CurrentUser.UserId
                };

                Db.Sys_FunctionPermissions.Add(functionPermission);
                permissionLookup[functionKey] = functionPermission;
                result.CreatedFunctionCount++;
            }
            else
            {
                result.ExistingFunctionCount++;
                functionPermission.FunctionName = menu.MenuName.Trim();
                functionPermission.SortOrder = menu.SortOrder;
                functionPermission.IsEnable = menu.IsEnable;
                functionPermission.UpdatedTime = now;
                functionPermission.UpdatedId = CurrentUser.UserId;
            }

            functionPermissionsByMenuId[menu.Id] = functionPermission;
        }

        await Db.SaveChangesAsync();

        foreach (var menu in menus.Where(m => !string.IsNullOrWhiteSpace(m.MenuCode)))
        {
            var functionKey = menu.MenuCode.Trim();
            var functionPermission = functionPermissionsByMenuId[menu.Id];
            functionPermission.ParentFunctionPermissionId =
                menu.ParentId.HasValue && functionPermissionsByMenuId.TryGetValue(menu.ParentId.Value, out var parentPermission)
                    ? parentPermission.FunctionPermissionId
                    : null;

            foreach (var operation in operations)
            {
                var operationKey = BuildPermissionKey(functionKey, operation.Code);
                if (!permissionLookup.TryGetValue(operationKey, out var operationPermission))
                {
                    operationPermission = new Sys_FunctionPermission
                    {
                        ParentFunctionPermissionId = functionPermission.FunctionPermissionId,
                        PermissionKey = operationKey,
                        FunctionCode = functionKey,
                        FunctionName = menu.MenuName.Trim(),
                        OperationCode = operation.Code,
                        OperationName = operation.Name,
                        SortOrder = operation.SortOrder,
                        IsEnable = menu.IsEnable,
                        CreatedTime = now,
                        CreatedId = CurrentUser.UserId,
                        UpdatedTime = now,
                        UpdatedId = CurrentUser.UserId
                    };

                    Db.Sys_FunctionPermissions.Add(operationPermission);
                    permissionLookup[operationKey] = operationPermission;
                    result.CreatedOperationCount++;
                }
                else
                {
                    result.ExistingOperationCount++;
                    operationPermission.ParentFunctionPermissionId = functionPermission.FunctionPermissionId;
                    operationPermission.FunctionName = menu.MenuName.Trim();
                    operationPermission.OperationName = operation.Name;
                    operationPermission.SortOrder = operation.SortOrder;
                    operationPermission.IsEnable = menu.IsEnable;
                    operationPermission.UpdatedTime = now;
                    operationPermission.UpdatedId = CurrentUser.UserId;
                }
            }
        }

        await Db.SaveChangesAsync();
        if (transaction is not null)
            await transaction.CommitAsync();

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FunctionPermissionDto>> GetRoleGroupPermissionsAsync(int roleGroupId, bool? isEnable)
    {
        if (roleGroupId <= 0)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.RoleGroupIdMustBeGreaterThanZero), nameof(roleGroupId));

        return await GetAssignedPermissionsAsync([roleGroupId], isEnable, includeAncestors: false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FunctionPermissionDto>> GetRoleGroupPermissionTreeAsync(int roleGroupId, bool? isEnable)
    {
        if (roleGroupId <= 0)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.RoleGroupIdMustBeGreaterThanZero), nameof(roleGroupId));

        var permissions = await GetAssignedPermissionsAsync([roleGroupId], isEnable, includeAncestors: true);
        return TreeBuilder.BuildTree(
            permissions,
            p => p.FunctionPermissionId,
            p => p.ParentFunctionPermissionId,
            CloneWithoutChildren,
            p => p.Children,
            ComparePermissions);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateRoleGroupPermissionsAsync(RoleGroupFunctionPermissionUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RoleGroupId <= 0)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.RoleGroupIdMustBeGreaterThanZero), nameof(request.RoleGroupId));

        var roleGroupExists = await Db.Sys_RoleGroups.AnyAsync(r => r.RoleGroupId == request.RoleGroupId);
        if (!roleGroupExists)
            return false;

        var requestPermissionIds = request.FunctionPermissionIds ?? [];
        if (requestPermissionIds.Any(id => id <= 0))
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.FunctionPermissionIdsMustBeGreaterThanZero), nameof(request.FunctionPermissionIds));

        if (requestPermissionIds.Count != requestPermissionIds.Distinct().Count())
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.FunctionPermissionIdsDuplicate), nameof(request.FunctionPermissionIds));

        var functionPermissionIds = requestPermissionIds
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        var existingPermissionIds = await Db.Sys_FunctionPermissions
            .Where(p => functionPermissionIds.Contains(p.FunctionPermissionId))
            .Select(p => p.FunctionPermissionId)
            .ToListAsync();

        if (existingPermissionIds.Count != functionPermissionIds.Count)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.FunctionPermissionIdsNotFound), nameof(request.FunctionPermissionIds));

        var currentMappings = await Db.Sys_RoleGroupFunctionPermissions
            .Where(p => p.RoleGroupId == request.RoleGroupId)
            .ToListAsync();
        Db.Sys_RoleGroupFunctionPermissions.RemoveRange(currentMappings);

        var now = DateTime.UtcNow;
        foreach (var functionPermissionId in functionPermissionIds)
        {
            Db.Sys_RoleGroupFunctionPermissions.Add(new Sys_RoleGroupFunctionPermission
            {
                RoleGroupId = request.RoleGroupId,
                FunctionPermissionId = functionPermissionId,
                CreatedTime = now,
                CreatedId = CurrentUser.UserId
            });
        }

        await Db.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FunctionPermissionDto>> GetUserPermissionTreeAsync(string userId, bool? isEnable)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.UserIdRequired), nameof(userId));

        var roleGroupIds = await Db.Sys_UserRoleGroups
            .AsNoTracking()
            .Where(r => r.UserId == userId.Trim())
            .Select(r => r.RoleGroupId)
            .Distinct()
            .ToListAsync();

        var permissions = await GetAssignedPermissionsAsync(roleGroupIds, isEnable, includeAncestors: true);
        return TreeBuilder.BuildTree(
            permissions,
            p => p.FunctionPermissionId,
            p => p.ParentFunctionPermissionId,
            CloneWithoutChildren,
            p => p.Children,
            ComparePermissions);
    }

    private async Task<IReadOnlyList<FunctionPermissionDto>> GetAssignedPermissionsAsync(
        IReadOnlyList<int> roleGroupIds,
        bool? isEnable,
        bool includeAncestors)
    {
        if (roleGroupIds.Count == 0)
            return [];

        var assignedIds = await Db.Sys_RoleGroupFunctionPermissions
            .AsNoTracking()
            .Where(p => roleGroupIds.Contains(p.RoleGroupId))
            .Select(p => p.FunctionPermissionId)
            .Distinct()
            .ToListAsync();

        if (assignedIds.Count == 0)
            return [];

        var includeIds = new HashSet<int>(assignedIds);

        if (includeAncestors)
        {
            var hierarchyQuery = Db.Sys_FunctionPermissions.AsNoTracking().AsQueryable();
            if (isEnable.HasValue)
                hierarchyQuery = hierarchyQuery.Where(p => p.IsEnable == isEnable.Value);

            var hierarchy = await hierarchyQuery
                .Select(p => new { p.FunctionPermissionId, p.ParentFunctionPermissionId })
                .ToListAsync();
            var lookup = hierarchy.ToDictionary(p => p.FunctionPermissionId);

            foreach (var assignedId in assignedIds)
            {
                var currentId = assignedId;
                while (lookup.TryGetValue(currentId, out var permission) && permission.ParentFunctionPermissionId.HasValue)
                {
                    currentId = permission.ParentFunctionPermissionId.Value;
                    includeIds.Add(currentId);
                }
            }
        }

        var permissionQuery = Db.Sys_FunctionPermissions
            .AsNoTracking()
            .Where(p => includeIds.Contains(p.FunctionPermissionId));
        if (isEnable.HasValue)
            permissionQuery = permissionQuery.Where(p => p.IsEnable == isEnable.Value);

        return await permissionQuery
            .Select(ToDtoExpression())
            .OrderBy(p => p.ParentFunctionPermissionId)
            .ThenBy(p => p.SortOrder)
            .ThenBy(p => p.FunctionPermissionId)
            .ToListAsync();
    }

    /// <summary>
    /// 檢查上層功能權限是否為有效既有權限；空值代表根權限。
    /// </summary>
    private async Task EnsureParentExistsAsync(int? parentFunctionPermissionId)
    {
        if (!parentFunctionPermissionId.HasValue)
            return;

        if (parentFunctionPermissionId.Value <= 0)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.ParentFunctionPermissionIdMustBeGreaterThanZero), nameof(parentFunctionPermissionId));

        var exists = await Db.Sys_FunctionPermissions.AnyAsync(p => p.FunctionPermissionId == parentFunctionPermissionId.Value);
        if (!exists)
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.ParentFunctionPermissionNotFound), nameof(parentFunctionPermissionId));
    }

    /// <summary>
    /// 檢查更新上層功能權限時不會移到自己的後代底下，避免樹狀循環。
    /// </summary>
    private async Task EnsureParentIsNotDescendantAsync(int functionPermissionId, int? parentFunctionPermissionId)
    {
        if (!parentFunctionPermissionId.HasValue)
            return;

        var parentLookup = await Db.Sys_FunctionPermissions
            .AsNoTracking()
            .Select(p => new { p.FunctionPermissionId, p.ParentFunctionPermissionId })
            .ToDictionaryAsync(p => p.FunctionPermissionId, p => p.ParentFunctionPermissionId);

        var currentParentId = parentFunctionPermissionId.Value;
        while (true)
        {
            if (currentParentId == functionPermissionId)
                throw new ArgumentException(Message(FunctionPermissionMessageEnum.ParentFunctionPermissionIdCannotBeDescendant), nameof(parentFunctionPermissionId));

            if (!parentLookup.TryGetValue(currentParentId, out var nextParentId) || nextParentId is null)
                return;

            currentParentId = nextParentId.Value;
        }
    }

    /// <summary>
    /// 取得指定功能權限底下所有後代權限 ID。
    /// </summary>
    private async Task<List<int>> GetDescendantIdsAsync(int functionPermissionId)
    {
        var permissions = await Db.Sys_FunctionPermissions
            .AsNoTracking()
            .Select(p => new { p.FunctionPermissionId, p.ParentFunctionPermissionId })
            .ToListAsync();

        var descendants = new List<int>();
        var pending = new Queue<int>();
        pending.Enqueue(functionPermissionId);

        while (pending.Count > 0)
        {
            var parentId = pending.Dequeue();
            foreach (var child in permissions.Where(p => p.ParentFunctionPermissionId == parentId))
            {
                descendants.Add(child.FunctionPermissionId);
                pending.Enqueue(child.FunctionPermissionId);
            }
        }

        return descendants;
    }

    /// <summary>
    /// 驗證功能權限代碼、名稱與操作代碼等必要欄位。
    /// </summary>
    private static void ValidateRequest(string functionCode, string functionName, string? operationCode)
    {
        if (string.IsNullOrWhiteSpace(functionCode))
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.FunctionCodeRequired), nameof(functionCode));

        if (string.IsNullOrWhiteSpace(functionName))
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.FunctionNameRequired), nameof(functionName));

        _ = NormalizeOperationCode(operationCode);
    }

    /// <summary>
    /// 正規化操作代碼；空白值視為沒有操作代碼。
    /// </summary>
    private static string? NormalizeOperationCode(string? operationCode)
    {
        if (string.IsNullOrWhiteSpace(operationCode))
            return null;

        var normalized = operationCode.Trim().ToUpperInvariant();
        if (!Enum.TryParse<FunctionOperationCode>(normalized, out _))
            throw new ArgumentException(Message(FunctionPermissionMessageEnum.OperationCodeInvalid), nameof(operationCode));

        return normalized;
    }

    /// <summary>
    /// 依功能代碼與操作代碼組成權限唯一鍵。
    /// </summary>
    private static string BuildPermissionKey(string functionCode, string? operationCode)
    {
        var normalizedFunctionCode = functionCode.Trim();
        return string.IsNullOrWhiteSpace(operationCode)
            ? normalizedFunctionCode
            : $"{normalizedFunctionCode}:{operationCode}";
    }

    /// <summary>
    /// 依操作代碼取得顯示名稱。
    /// </summary>
    private static string GetOperationName(string? operationCode)
    {
        if (string.IsNullOrWhiteSpace(operationCode))
            return string.Empty;

        return Enum.Parse<FunctionOperationCode>(operationCode).GetDescription();
    }

    /// <summary>
    /// 取得系統支援的標準操作代碼、名稱與排序。
    /// </summary>
    private static IReadOnlyList<(string Code, string Name, int SortOrder)> GetOperationDefinitions()
    {
        return Enum.GetValues<FunctionOperationCode>()
            .Select(operation => (operation.ToString(), operation.GetDescription(), Convert.ToInt32(operation)))
            .ToList();
    }

    /// <summary>
    /// 取得功能權限訊息列舉的描述文字。
    /// </summary>
    private static string Message(FunctionPermissionMessageEnum message) => message.GetDescription();

    private Task WriteFunctionPermissionLogAsync(
        AuditActionEnum action,
        int targetId,
        string message,
        object? oldValue = null,
        object? newValue = null,
        object? metadata = null)
    {
        return _logService.Value?.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "FunctionPermission",
            Action = action,
            Result = AuditResultEnum.Success,
            TargetType = nameof(Sys_FunctionPermission),
            TargetId = targetId.ToString(),
            Message = message,
            OldValue = oldValue,
            NewValue = newValue,
            Metadata = metadata
        }) ?? Task.CompletedTask;
    }

    private static int ComparePermissions(FunctionPermissionDto left, FunctionPermissionDto right)
    {
        var sortOrder = left.SortOrder.CompareTo(right.SortOrder);
        return sortOrder != 0 ? sortOrder : left.FunctionPermissionId.CompareTo(right.FunctionPermissionId);
    }

    /// <summary>
    /// 複製功能權限 DTO 並清空 Children，避免組樹時重用原集合。
    /// </summary>
    private static FunctionPermissionDto CloneWithoutChildren(FunctionPermissionDto permission)
    {
        return new FunctionPermissionDto
        {
            FunctionPermissionId = permission.FunctionPermissionId,
            ParentFunctionPermissionId = permission.ParentFunctionPermissionId,
            PermissionKey = permission.PermissionKey,
            FunctionCode = permission.FunctionCode,
            FunctionName = permission.FunctionName,
            OperationCode = permission.OperationCode,
            OperationName = permission.OperationName,
            SortOrder = permission.SortOrder,
            IsEnable = permission.IsEnable,
            CreatedTime = permission.CreatedTime,
            CreatedId = permission.CreatedId,
            UpdatedTime = permission.UpdatedTime,
            UpdatedId = permission.UpdatedId
        };
    }

    /// <summary>
    /// 將功能權限資料表實體轉成輸出 DTO。
    /// </summary>
    private static FunctionPermissionDto MapToDto(Sys_FunctionPermission permission)
    {
        return new FunctionPermissionDto
        {
            FunctionPermissionId = permission.FunctionPermissionId,
            ParentFunctionPermissionId = permission.ParentFunctionPermissionId,
            PermissionKey = permission.PermissionKey,
            FunctionCode = permission.FunctionCode,
            FunctionName = permission.FunctionName,
            OperationCode = permission.OperationCode,
            OperationName = permission.OperationName,
            SortOrder = permission.SortOrder,
            IsEnable = permission.IsEnable,
            CreatedTime = permission.CreatedTime,
            CreatedId = permission.CreatedId,
            UpdatedTime = permission.UpdatedTime,
            UpdatedId = permission.UpdatedId
        };
    }

    /// <summary>
    /// 建立 EF Core 可轉譯的功能權限實體到 DTO 投影運算式。
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<Sys_FunctionPermission, FunctionPermissionDto>> ToDtoExpression()
    {
        return permission => new FunctionPermissionDto
        {
            FunctionPermissionId = permission.FunctionPermissionId,
            ParentFunctionPermissionId = permission.ParentFunctionPermissionId,
            PermissionKey = permission.PermissionKey,
            FunctionCode = permission.FunctionCode,
            FunctionName = permission.FunctionName,
            OperationCode = permission.OperationCode,
            OperationName = permission.OperationName,
            SortOrder = permission.SortOrder,
            IsEnable = permission.IsEnable,
            CreatedTime = permission.CreatedTime,
            CreatedId = permission.CreatedId,
            UpdatedTime = permission.UpdatedTime,
            UpdatedId = permission.UpdatedId
        };
    }
}
