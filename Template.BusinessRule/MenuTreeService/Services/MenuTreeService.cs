using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.BusinessRule.Extensions;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.Common.Enums;
using Template.BusinessRule.MenuTreeService.Models;
using Template.Common.Models;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.MenuTreeService.Services;

/// <summary>
/// 選單樹商業邏輯服務。
/// </summary>
public class MenuTreeService(IServiceProvider serviceProvider) : BaseService(serviceProvider), IMenuTreeService
{
    private readonly Lazy<ILogService?> _logService = new(() => serviceProvider.GetService<ILogService>());

    /// <inheritdoc />
    public async Task<PageListOutput<MenuTreeDto>> GetListAsync(
        string? keyword,
        bool? isEnable,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50)
    {
        if (enablePaging)
            PageListQueryableExtensions.ValidatePaging(page, pageSize);

        var query = Db.Sys_MenuTrees.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(m =>
                m.MenuCode.Contains(k) ||
                m.MenuName.Contains(k));
        }

        if (isEnable.HasValue)
            query = query.Where(m => m.IsEnable == isEnable.Value);

        return await query
            .OrderBy(m => m.ParentId)
            .ThenBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .Select(ToDtoExpression())
            .ToPageListOutputAsync(page, pageSize, enablePaging);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuTreeDto>> GetTreeAsync(bool? isEnable)
    {
        var query = Db.Sys_MenuTrees.AsNoTracking().AsQueryable();

        if (isEnable.HasValue)
            query = query.Where(m => m.IsEnable == isEnable.Value);

        var menus = await query
            .OrderBy(m => m.ParentId)
            .ThenBy(m => m.SortOrder)
            .ThenBy(m => m.Id)
            .Select(ToDtoExpression())
            .ToListAsync();

        return TreeBuilder.BuildTree(
            menus,
            m => m.Id,
            m => m.ParentId,
            CloneWithoutChildren,
            m => m.Children,
            CompareMenus);
    }

    /// <inheritdoc />
    public async Task<MenuTreeDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id 必須大於 0。", nameof(id));

        return await Db.Sys_MenuTrees
            .AsNoTracking()
            .Where(m => m.Id == id)
            .Select(ToDtoExpression())
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<MenuTreeDto> CreateAsync(MenuTreeCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request.MenuCode, request.MenuName);

        var menuCode = request.MenuCode.Trim();
        var exists = await Db.Sys_MenuTrees.AnyAsync(m => m.MenuCode == menuCode);
        if (exists)
            throw new ArgumentException("MenuCode 已存在。", nameof(request.MenuCode));

        await EnsureParentExistsAsync(request.ParentId);

        var now = DateTime.UtcNow;
        var entity = new Sys_MenuTree
        {
            ParentId = request.ParentId,
            MenuCode = menuCode,
            MenuName = request.MenuName.Trim(),
            Icon = request.Icon.Trim(),
            SortOrder = request.SortOrder,
            IsEnable = request.IsEnable,
            CreatedTime = now,
            CreatedId = CurrentUser.UserId,
            UpdatedTime = now,
            UpdatedId = CurrentUser.UserId
        };

        Db.Sys_MenuTrees.Add(entity);
        await Db.SaveChangesAsync();

        await WriteMenuTreeLogAsync(
            AuditActionEnum.Create,
            entity.Id,
            "建立選單。",
            newValue: MapToDto(entity));

        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(MenuTreeUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id <= 0)
            throw new ArgumentException("Id 必須大於 0。", nameof(request.Id));

        ValidateRequest(request.MenuCode, request.MenuName);

        if (request.ParentId == request.Id)
            throw new ArgumentException("ParentId 不可與 Id 相同。", nameof(request.ParentId));

        var entity = await Db.Sys_MenuTrees.FirstOrDefaultAsync(m => m.Id == request.Id);
        if (entity is null)
            return false;

        var oldValue = MapToDto(entity);

        var menuCode = request.MenuCode.Trim();
        var codeExists = await Db.Sys_MenuTrees.AnyAsync(m => m.Id != request.Id && m.MenuCode == menuCode);
        if (codeExists)
            throw new ArgumentException("MenuCode 已存在。", nameof(request.MenuCode));

        await EnsureParentExistsAsync(request.ParentId);
        await EnsureParentIsNotDescendantAsync(request.Id, request.ParentId);

        entity.ParentId = request.ParentId;
        entity.MenuCode = menuCode;
        entity.MenuName = request.MenuName.Trim();
        entity.Icon = request.Icon.Trim();
        entity.SortOrder = request.SortOrder;
        entity.IsEnable = request.IsEnable;
        entity.UpdatedTime = DateTime.UtcNow;
        entity.UpdatedId = CurrentUser.UserId;

        await Db.SaveChangesAsync();

        await WriteMenuTreeLogAsync(
            AuditActionEnum.Update,
            entity.Id,
            "更新選單。",
            oldValue,
            MapToDto(entity));

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id 必須大於 0。", nameof(id));

        var entity = await Db.Sys_MenuTrees.FirstOrDefaultAsync(m => m.Id == id);
        if (entity is null)
            return false;

        var oldValue = MapToDto(entity);

        var hasChildren = await Db.Sys_MenuTrees.AnyAsync(m => m.ParentId == id);
        if (hasChildren)
            throw new InvalidOperationException("選單仍有子選單，無法刪除。");

        Db.Sys_MenuTrees.Remove(entity);
        await Db.SaveChangesAsync();

        await WriteMenuTreeLogAsync(
            AuditActionEnum.Delete,
            id,
            "刪除選單。",
            oldValue: oldValue);

        return true;
    }

    /// <summary>
    /// 檢查上層選單是否為有效既有選單；空值代表根選單。
    /// </summary>
    private async Task EnsureParentExistsAsync(int? parentId)
    {
        if (!parentId.HasValue)
            return;

        if (parentId.Value <= 0)
            throw new ArgumentException("ParentId 必須大於 0。", nameof(parentId));

        var exists = await Db.Sys_MenuTrees.AnyAsync(m => m.Id == parentId.Value);
        if (!exists)
            throw new ArgumentException("父層選單不存在。", nameof(parentId));
    }

    /// <summary>
    /// 檢查更新上層選單時不會把選單移到自己的後代底下，避免樹狀循環。
    /// </summary>
    private async Task EnsureParentIsNotDescendantAsync(int id, int? parentId)
    {
        if (!parentId.HasValue)
            return;

        var menus = await Db.Sys_MenuTrees
            .AsNoTracking()
            .Select(m => new { m.Id, m.ParentId })
            .ToListAsync();

        var currentParentId = parentId.Value;
        while (true)
        {
            if (currentParentId == id)
                throw new ArgumentException("ParentId 不可設定為該選單的子孫節點。", nameof(parentId));

            var parent = menus.FirstOrDefault(m => m.Id == currentParentId);
            if (parent?.ParentId is null)
                return;

            currentParentId = parent.ParentId.Value;
        }
    }

    /// <summary>
    /// 驗證選單代碼與名稱。
    /// </summary>
    private static void ValidateRequest(string menuCode, string menuName)
    {
        if (string.IsNullOrWhiteSpace(menuCode))
            throw new ArgumentException("MenuCode 不可為空。", nameof(menuCode));

        if (string.IsNullOrWhiteSpace(menuName))
            throw new ArgumentException("MenuName 不可為空。", nameof(menuName));
    }

    private Task WriteMenuTreeLogAsync(
        AuditActionEnum action,
        int targetId,
        string message,
        object? oldValue = null,
        object? newValue = null,
        object? metadata = null)
    {
        return _logService.Value?.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "MenuTree",
            Action = action,
            Result = AuditResultEnum.Success,
            TargetType = nameof(Sys_MenuTree),
            TargetId = targetId.ToString(),
            Message = message,
            OldValue = oldValue,
            NewValue = newValue,
            Metadata = metadata
        }) ?? Task.CompletedTask;
    }

    private static int CompareMenus(MenuTreeDto left, MenuTreeDto right)
    {
        var sortOrder = left.SortOrder.CompareTo(right.SortOrder);
        return sortOrder != 0 ? sortOrder : left.Id.CompareTo(right.Id);
    }

    /// <summary>
    /// 複製選單 DTO 並清空 Children，避免組樹時重用原集合。
    /// </summary>
    private static MenuTreeDto CloneWithoutChildren(MenuTreeDto menu)
    {
        return new MenuTreeDto
        {
            Id = menu.Id,
            ParentId = menu.ParentId,
            MenuCode = menu.MenuCode,
            MenuName = menu.MenuName,
            Icon = menu.Icon,
            SortOrder = menu.SortOrder,
            IsEnable = menu.IsEnable,
            CreatedTime = menu.CreatedTime,
            CreatedId = menu.CreatedId,
            UpdatedTime = menu.UpdatedTime,
            UpdatedId = menu.UpdatedId
        };
    }

    /// <summary>
    /// 將選單資料表實體轉成輸出 DTO。
    /// </summary>
    private static MenuTreeDto MapToDto(Sys_MenuTree menu)
    {
        return new MenuTreeDto
        {
            Id = menu.Id,
            ParentId = menu.ParentId,
            MenuCode = menu.MenuCode,
            MenuName = menu.MenuName,
            Icon = menu.Icon,
            SortOrder = menu.SortOrder,
            IsEnable = menu.IsEnable,
            CreatedTime = menu.CreatedTime,
            CreatedId = menu.CreatedId,
            UpdatedTime = menu.UpdatedTime,
            UpdatedId = menu.UpdatedId
        };
    }

    /// <summary>
    /// 建立 EF Core 可轉譯的選單實體到 DTO 投影運算式。
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<Sys_MenuTree, MenuTreeDto>> ToDtoExpression()
    {
        return menu => new MenuTreeDto
        {
            Id = menu.Id,
            ParentId = menu.ParentId,
            MenuCode = menu.MenuCode,
            MenuName = menu.MenuName,
            Icon = menu.Icon,
            SortOrder = menu.SortOrder,
            IsEnable = menu.IsEnable,
            CreatedTime = menu.CreatedTime,
            CreatedId = menu.CreatedId,
            UpdatedTime = menu.UpdatedTime,
            UpdatedId = menu.UpdatedId
        };
    }
}
