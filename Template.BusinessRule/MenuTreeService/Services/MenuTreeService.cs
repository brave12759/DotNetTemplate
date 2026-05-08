using Microsoft.EntityFrameworkCore;
using Template.BusinessRule.MenuTreeService.Models;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.MenuTreeService.Services;

/// <summary>
/// 選單樹商業邏輯服務。
/// </summary>
public class MenuTreeService(IServiceProvider serviceProvider) : BaseService(serviceProvider), IMenuTreeService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuTreeDto>> GetListAsync(string? keyword, bool? isEnable)
    {
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
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<MenuTreeDto>> GetTreeAsync(bool? isEnable)
    {
        var menus = await GetListAsync(null, isEnable);
        return BuildTree(menus);
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

        var hasChildren = await Db.Sys_MenuTrees.AnyAsync(m => m.ParentId == id);
        if (hasChildren)
            throw new InvalidOperationException("選單仍有子選單，無法刪除。");

        Db.Sys_MenuTrees.Remove(entity);
        await Db.SaveChangesAsync();
        return true;
    }

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

    private static void ValidateRequest(
        string menuCode,
        string menuName)
    {
        if (string.IsNullOrWhiteSpace(menuCode))
            throw new ArgumentException("MenuCode 為必填。", nameof(menuCode));

        if (string.IsNullOrWhiteSpace(menuName))
            throw new ArgumentException("MenuName 為必填。", nameof(menuName));
    }

    private static List<MenuTreeDto> BuildTree(IReadOnlyList<MenuTreeDto> menus)
    {
        var lookup = menus.ToDictionary(m => m.Id, CloneWithoutChildren);
        var roots = new List<MenuTreeDto>();

        foreach (var menu in lookup.Values.OrderBy(m => m.ParentId).ThenBy(m => m.SortOrder).ThenBy(m => m.Id))
        {
            if (menu.ParentId.HasValue && lookup.TryGetValue(menu.ParentId.Value, out var parent))
                parent.Children.Add(menu);
            else
                roots.Add(menu);
        }

        SortChildren(roots);    
        return roots;
    }

    private static void SortChildren(List<MenuTreeDto> menus)
    {
        menus.Sort((left, right) =>
        {
            var sortOrder = left.SortOrder.CompareTo(right.SortOrder);
            return sortOrder != 0 ? sortOrder : left.Id.CompareTo(right.Id);
        });

        foreach (var menu in menus)
            SortChildren(menu.Children);
    }

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
