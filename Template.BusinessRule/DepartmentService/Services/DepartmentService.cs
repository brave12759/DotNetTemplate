using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.BusinessRule.DepartmentService.Models;
using Template.BusinessRule.Extensions;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.Common.Enums;
using Template.Common.Models;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.DepartmentService.Services;

/// <summary>
/// 部門服務，負責部門 CRUD、清單查詢與樹狀資料組裝。
/// </summary>
public class DepartmentService(IServiceProvider serviceProvider) : BaseService(serviceProvider), IDepartmentService
{
    private const int MaxPageSize = 200;

    private readonly Lazy<ILogService?> _logService = new(() => serviceProvider.GetService<ILogService>());

    /// <inheritdoc />
    public async Task<PageListOutput<DepartmentDto>> GetListAsync(
        string? keyword,
        bool? isEnable,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50)
    {
        if (enablePaging)
            PageListQueryableExtensions.ValidatePaging(page, pageSize);

        var query = Db.Sys_Departments.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(d => d.DeptName.Contains(k));
        }

        if (isEnable.HasValue)
            query = query.Where(d => d.IsEnable == isEnable.Value);

        return await query
            .OrderBy(d => d.ParentDeptId)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.DeptId)
            .Select(ToDtoExpression())
            .ToPageListOutputAsync(page, pageSize, enablePaging);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DepartmentDto>> GetTreeAsync(bool? isEnable)
    {
        var query = Db.Sys_Departments.AsNoTracking().AsQueryable();

        if (isEnable.HasValue)
            query = query.Where(d => d.IsEnable == isEnable.Value);

        var departments = await query
            .OrderBy(d => d.ParentDeptId)
            .ThenBy(d => d.SortOrder)
            .ThenBy(d => d.DeptId)
            .Select(ToDtoExpression())
            .ToListAsync();

        return TreeBuilder.BuildTree(
            departments,
            d => d.DeptId,
            d => d.ParentDeptId,
            CloneWithoutChildren,
            d => d.Children,
            CompareDepartments);
    }

    /// <inheritdoc />
    public async Task<DepartmentDto?> GetByIdAsync(int deptId)
    {
        if (deptId <= 0)
            throw new ArgumentException("DeptId must be greater than zero.", nameof(deptId));

        return await Db.Sys_Departments
            .AsNoTracking()
            .Where(d => d.DeptId == deptId)
            .Select(ToDtoExpression())
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<DepartmentDto> CreateAsync(DepartmentCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequest(request.DeptName);
        await EnsureParentExistsAsync(request.ParentDeptId);

        var now = DateTime.UtcNow;
        var entity = new Sys_Department
        {
            DeptName = request.DeptName.Trim(),
            ParentDeptId = request.ParentDeptId,
            SortOrder = request.SortOrder,
            IsEnable = request.IsEnable,
            CreatedTime = now,
            CreatedId = CurrentUser.UserId,
            UpdatedTime = now,
            UpdatedId = CurrentUser.UserId
        };

        Db.Sys_Departments.Add(entity);
        await Db.SaveChangesAsync();
        await WriteDepartmentLogAsync(
            AuditActionEnum.Create,
            entity.DeptId,
            "建立部門。",
            newValue: MapToDto(entity));

        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(DepartmentUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.DeptId <= 0)
            throw new ArgumentException("DeptId must be greater than zero.", nameof(request.DeptId));

        ValidateRequest(request.DeptName);

        if (request.ParentDeptId == request.DeptId)
            throw new ArgumentException("ParentDeptId cannot equal DeptId.", nameof(request.ParentDeptId));

        var entity = await Db.Sys_Departments.FirstOrDefaultAsync(d => d.DeptId == request.DeptId);
        if (entity is null)
            return false;

        var oldValue = MapToDto(entity);
        await EnsureParentExistsAsync(request.ParentDeptId);
        await EnsureParentIsNotDescendantAsync(request.DeptId, request.ParentDeptId);

        entity.DeptName = request.DeptName.Trim();
        entity.ParentDeptId = request.ParentDeptId;
        entity.SortOrder = request.SortOrder;
        entity.IsEnable = request.IsEnable;
        entity.UpdatedTime = DateTime.UtcNow;
        entity.UpdatedId = CurrentUser.UserId;

        await Db.SaveChangesAsync();
        await WriteDepartmentLogAsync(
            AuditActionEnum.Update,
            entity.DeptId,
            "更新部門。",
            oldValue,
            MapToDto(entity));
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int deptId)
    {
        if (deptId <= 0)
            throw new ArgumentException("DeptId must be greater than zero.", nameof(deptId));

        var entity = await Db.Sys_Departments.FirstOrDefaultAsync(d => d.DeptId == deptId);
        if (entity is null)
            return false;

        var hasChildren = await Db.Sys_Departments.AnyAsync(d => d.ParentDeptId == deptId);
        if (hasChildren)
            throw new InvalidOperationException("Cannot delete a department that has child departments.");

        var hasUsers = await Db.Sys_UserInfos.AnyAsync(u => u.DeptId == deptId);
        if (hasUsers)
            throw new InvalidOperationException("Cannot delete a department that has users.");

        var oldValue = MapToDto(entity);
        Db.Sys_Departments.Remove(entity);
        await Db.SaveChangesAsync();
        await WriteDepartmentLogAsync(
            AuditActionEnum.Delete,
            entity.DeptId,
            "刪除部門。",
            oldValue: oldValue);
        return true;
    }

    /// <summary>
    /// 寫入部門操作日誌；測試未註冊 LogService 時略過。
    /// </summary>
    private Task WriteDepartmentLogAsync(
        AuditActionEnum action,
        int deptId,
        string message,
        object? oldValue = null,
        object? newValue = null)
    {
        return _logService.Value?.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "Department",
            Action = action,
            Result = AuditResultEnum.Success,
            TargetType = nameof(Sys_Department),
            TargetId = deptId.ToString(),
            Message = message,
            OldValue = oldValue,
            NewValue = newValue
        }) ?? Task.CompletedTask;
    }

    /// <summary>
    /// 有指定上層部門時，確認該上層部門存在。
    /// </summary>
    private async Task EnsureParentExistsAsync(int? parentDeptId)
    {
        if (!parentDeptId.HasValue)
            return;

        if (parentDeptId.Value <= 0)
            throw new ArgumentException("ParentDeptId must be greater than zero.", nameof(parentDeptId));

        var exists = await Db.Sys_Departments.AnyAsync(d => d.DeptId == parentDeptId.Value);
        if (!exists)
            throw new ArgumentException("Parent department does not exist.", nameof(parentDeptId));
    }

    /// <summary>
    /// 防止把部門移到自己的子孫部門底下，避免形成循環階層。
    /// </summary>
    private async Task EnsureParentIsNotDescendantAsync(int deptId, int? parentDeptId)
    {
        if (!parentDeptId.HasValue)
            return;

        var departments = await Db.Sys_Departments
            .AsNoTracking()
            .Select(d => new { d.DeptId, d.ParentDeptId })
            .ToListAsync();

        var currentParentDeptId = parentDeptId.Value;
        while (true)
        {
            if (currentParentDeptId == deptId)
                throw new ArgumentException("ParentDeptId cannot be a descendant department.", nameof(parentDeptId));

            var parent = departments.FirstOrDefault(d => d.DeptId == currentParentDeptId);
            if (parent?.ParentDeptId is null)
                return;

            currentParentDeptId = parent.ParentDeptId.Value;
        }
    }

    /// <summary>
    /// 驗證部門必要欄位。
    /// </summary>
    private static void ValidateRequest(string deptName)
    {
        if (string.IsNullOrWhiteSpace(deptName))
            throw new ArgumentException("DeptName is required.", nameof(deptName));
    }

    private static int CompareDepartments(DepartmentDto left, DepartmentDto right)
    {
        var sortOrder = left.SortOrder.CompareTo(right.SortOrder);
        return sortOrder != 0 ? sortOrder : left.DeptId.CompareTo(right.DeptId);
    }

    /// <summary>
    /// 複製部門 DTO，但不複製 Children，避免樹狀組裝時共用原始集合。
    /// </summary>
    private static DepartmentDto CloneWithoutChildren(DepartmentDto department)
    {
        return new DepartmentDto
        {
            DeptId = department.DeptId,
            DeptName = department.DeptName,
            ParentDeptId = department.ParentDeptId,
            SortOrder = department.SortOrder,
            IsEnable = department.IsEnable,
            CreatedTime = department.CreatedTime,
            CreatedId = department.CreatedId,
            UpdatedTime = department.UpdatedTime,
            UpdatedId = department.UpdatedId
        };
    }

    /// <summary>
    /// 將部門資料表實體轉成 DTO。
    /// </summary>
    private static DepartmentDto MapToDto(Sys_Department department)
    {
        return new DepartmentDto
        {
            DeptId = department.DeptId,
            DeptName = department.DeptName,
            ParentDeptId = department.ParentDeptId,
            SortOrder = department.SortOrder,
            IsEnable = department.IsEnable,
            CreatedTime = department.CreatedTime,
            CreatedId = department.CreatedId,
            UpdatedTime = department.UpdatedTime,
            UpdatedId = department.UpdatedId
        };
    }

    /// <summary>
    /// 建立 EF Core 查詢投影，讓部門查詢直接產生 DTO。
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<Sys_Department, DepartmentDto>> ToDtoExpression()
    {
        return department => new DepartmentDto
        {
            DeptId = department.DeptId,
            DeptName = department.DeptName,
            ParentDeptId = department.ParentDeptId,
            SortOrder = department.SortOrder,
            IsEnable = department.IsEnable,
            CreatedTime = department.CreatedTime,
            CreatedId = department.CreatedId,
            UpdatedTime = department.UpdatedTime,
            UpdatedId = department.UpdatedId
        };
    }
}
