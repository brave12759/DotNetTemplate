using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.BusinessRule.Extensions;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.PasswordManager.Services;
using Template.Common.Enums;
using Template.Common.Models;
using Template.Common.Models.User;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.UserService.Services;

/// <summary>
/// 使用者 CRUD、密碼維護與部門歸屬查詢服務。
/// </summary>
public class UserService(IServiceProvider serviceProvider) : BaseService(serviceProvider), IUserService
{
    private readonly Lazy<IPasswordManager> _passwordManager = new(() =>
        serviceProvider.GetRequiredService<IPasswordManager>());

    private readonly Lazy<ILogService> _logService = new(() =>
        serviceProvider.GetRequiredService<ILogService>());

    /// <inheritdoc />
    public async Task<PageListOutput<UserDto>> GetListAsync(
        string? keyword,
        bool? isEnable,
        int? deptId = null,
        bool includeSubDepartments = false,
        bool enablePaging = false,
        int page = 1,
        int pageSize = 50)
    {
        if (enablePaging)
            PageListQueryableExtensions.ValidatePaging(page, pageSize);

        var query = Db.Sys_UserInfos.AsNoTracking().AsQueryable();

        if (deptId.HasValue)
        {
            if (deptId.Value <= 0)
                throw new ArgumentException("DeptId must be greater than zero.", nameof(deptId));

            if (includeSubDepartments)
            {
                var deptIds = await GetDepartmentAndDescendantIdsAsync(deptId.Value);
                query = query.Where(u => deptIds.Contains(u.DeptId));
            }
            else
            {
                await EnsureDepartmentExistsAsync(deptId.Value);
                query = query.Where(u => u.DeptId == deptId.Value);
            }
        }

        if (isEnable.HasValue)
            query = query.Where(u => u.IsEnable == isEnable.Value);

        var projected = ApplyDepartmentJoin(query);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            var hasDeptIdKeyword = int.TryParse(k, out var keywordDeptId);
            projected = projected.Where(u =>
                u.UserId.Contains(k) ||
                u.UserName.Contains(k) ||
                u.Email.Contains(k) ||
                u.MobilePhone.Contains(k) ||
                u.DeptName.Contains(k) ||
                (hasDeptIdKeyword && u.DeptId == keywordDeptId));
        }

        return await projected
            .OrderBy(u => u.Id)
            .ToPageListOutputAsync(page, pageSize, enablePaging);
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id must be greater than zero.", nameof(id));

        var query = Db.Sys_UserInfos
            .AsNoTracking()
            .Where(u => u.Id == id);

        return await ApplyDepartmentJoin(query).FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateAsync(UserCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("UserId is required.", nameof(request.UserId));

        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new ArgumentException("UserName is required.", nameof(request.UserName));

        await EnsureDepartmentExistsAsync(request.DeptId);

        var userId = request.UserId.Trim();
        var exists = await Db.Sys_UserInfos.AnyAsync(u => u.UserId == userId);
        if (exists)
            throw new ArgumentException("UserId already exists.", nameof(request.UserId));

        var password = string.IsNullOrWhiteSpace(request.Password)
            ? await GetDefaultPasswordAsync()
            : request.Password;

        var now = DateTime.UtcNow;
        var createdBy = CurrentUserService?.CurrentUser?.UserId ?? "system";
        var passwordHash = _passwordManager.Value.HashForStorage(password);
        var entity = new Sys_UserInfo
        {
            UserId = userId,
            UserName = request.UserName.Trim(),
            Password = passwordHash,
            DeptId = request.DeptId,
            MobilePhone = request.MobilePhone.Trim(),
            Email = request.Email.Trim(),
            IsEnable = request.IsEnable,
            LoginFailCount = 0,
            LastLoginIp = null,
            LastLoginTime = null,
            CreatedTime = now,
            CreatedId = createdBy,
            UpdatedTime = now,
            UpdatedId = createdBy
        };

        Db.Sys_UserInfos.Add(entity);
        AddPasswordHistory(userId, passwordHash, UserPasswordChangeTypeEnum.Create, now, createdBy);
        await Db.SaveChangesAsync();
        await WriteUserAuditAsync(
            AuditActionEnum.Create,
            userId,
            "建立使用者。",
            newValue: CreateSafeUserSnapshot(entity),
            metadata: new { PasswordChangeType = UserPasswordChangeTypeEnum.Create.ToString() });

        return await GetByIdAsync(entity.Id) ?? MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(UserUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id <= 0)
            throw new ArgumentException("Id must be greater than zero.", nameof(request.Id));

        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new ArgumentException("UserName is required.", nameof(request.UserName));

        await EnsureDepartmentExistsAsync(request.DeptId);

        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.Id == request.Id);
        if (user is null)
            return false;

        var oldValue = CreateSafeUserSnapshot(user);

        user.UserName = request.UserName.Trim();
        user.DeptId = request.DeptId;
        user.MobilePhone = request.MobilePhone.Trim();
        user.Email = request.Email.Trim();
        user.IsEnable = request.IsEnable;
        user.UpdatedTime = DateTime.UtcNow;
        user.UpdatedId = CurrentUserService?.CurrentUser?.UserId ?? "system";

        await Db.SaveChangesAsync();
        await WriteUserAuditAsync(
            AuditActionEnum.Update,
            user.UserId,
            "更新使用者基本資料。",
            oldValue,
            CreateSafeUserSnapshot(user));
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id must be greater than zero.", nameof(id));

        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return false;

        var oldValue = CreateSafeUserSnapshot(user);
        Db.Sys_UserInfos.Remove(user);
        await Db.SaveChangesAsync();
        await WriteUserAuditAsync(
            AuditActionEnum.Delete,
            user.UserId,
            "刪除使用者。",
            oldValue: oldValue);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordAsync(UserResetPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id <= 0)
            throw new ArgumentException("Id must be greater than zero.", nameof(request.Id));

        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.Id == request.Id);
        if (user is null)
            return false;

        var newPassword = string.IsNullOrWhiteSpace(request.NewPassword)
            ? await GetDefaultPasswordAsync()
            : request.NewPassword;

        var now = DateTime.UtcNow;
        var updatedBy = CurrentUserService?.CurrentUser?.UserId ?? "system";
        var passwordHash = _passwordManager.Value.HashForStorage(newPassword);

        user.Password = passwordHash;
        user.LoginFailCount = 0;
        user.IsEnable = true;
        user.UpdatedTime = now;
        user.UpdatedId = updatedBy;
        AddPasswordHistory(user.UserId, passwordHash, UserPasswordChangeTypeEnum.Reset, now, updatedBy);

        await Db.SaveChangesAsync();
        await WriteUserAuditAsync(
            AuditActionEnum.PasswordReset,
            user.UserId,
            "重設使用者密碼。",
            metadata: new { PasswordChangeType = UserPasswordChangeTypeEnum.Reset.ToString() });
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(UserChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id <= 0)
            throw new ArgumentException("Id must be greater than zero.", nameof(request.Id));

        if (string.IsNullOrWhiteSpace(request.OldPassword))
            throw new ArgumentException("OldPassword is required.", nameof(request.OldPassword));

        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.Id == request.Id);
        if (user is null)
            return false;

        if (!_passwordManager.Value.Verify(request.OldPassword, user.Password))
            throw new UnauthorizedAccessException("OldPassword is invalid.");

        _passwordManager.Value.ValidateNewPassword(request.NewPassword);

        var now = DateTime.UtcNow;
        var updatedBy = CurrentUserService?.CurrentUser?.UserId ?? "system";
        var passwordHash = _passwordManager.Value.HashForStorage(request.NewPassword);

        user.Password = passwordHash;
        user.UpdatedTime = now;
        user.UpdatedId = updatedBy;
        AddPasswordHistory(user.UserId, passwordHash, UserPasswordChangeTypeEnum.Change, now, updatedBy);

        await Db.SaveChangesAsync();
        await WriteUserAuditAsync(
            AuditActionEnum.PasswordChange,
            user.UserId,
            "使用者變更密碼。",
            metadata: new { PasswordChangeType = UserPasswordChangeTypeEnum.Change.ToString() });
        return true;
    }

    /// <summary>
    /// 從系統設定取得預設密碼，用於建立使用者或重設密碼未指定密碼時。
    /// </summary>
    private async Task<string> GetDefaultPasswordAsync()
    {
        var setting = await Db.Sys_BasicSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s =>
                s.Type == SystemSettingTypeEnum.SystemSetting.ToSettingTypeValue() &&
                s.Key == SystemSettingKeyEnum.DefaultPassword.ToSettingKeyValue());

        if (string.IsNullOrWhiteSpace(setting?.Value))
            throw new InvalidOperationException("Default password is not configured.");

        return setting.Value;
    }

    /// <summary>
    /// 寫入使用者密碼歷程，讓密碼過期與日後禁止重複使用密碼可依歷程判斷。
    /// </summary>
    private void AddPasswordHistory(
        string userId,
        string passwordHash,
        UserPasswordChangeTypeEnum changeType,
        DateTime changedTime,
        string changedBy)
    {
        Db.Sys_UserPasswordHistories.Add(new Sys_UserPasswordHistory
        {
            UserId = userId,
            PasswordHash = passwordHash,
            ChangeType = (int)changeType,
            ChangedTime = changedTime,
            ChangedId = changedBy
        });
    }

    /// <summary>
    /// 寫入使用者相關稽核日誌；此處刻意不記錄密碼雜湊或明文密碼。
    /// </summary>
    private Task WriteUserAuditAsync(
        AuditActionEnum action,
        string targetUserId,
        string message,
        object? oldValue = null,
        object? newValue = null,
        object? metadata = null)
    {
        return _logService.Value.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "User",
            Action = action,
            Result = AuditResultEnum.Success,
            TargetType = nameof(Sys_UserInfo),
            TargetId = targetUserId,
            Message = message,
            OldValue = oldValue,
            NewValue = newValue,
            Metadata = metadata
        });
    }

    /// <summary>
    /// 建立可寫入日誌的使用者快照，只保留非敏感欄位。
    /// </summary>
    private static object CreateSafeUserSnapshot(Sys_UserInfo user)
    {
        return new
        {
            user.Id,
            user.UserId,
            user.UserName,
            user.DeptId,
            user.MobilePhone,
            user.Email,
            user.IsEnable,
            user.LoginFailCount,
            user.CreatedTime,
            user.CreatedId,
            user.UpdatedTime,
            user.UpdatedId
        };
    }

    /// <summary>
    /// 檢查使用者指定的部門 ID 是否為有效既有部門。
    /// </summary>
    private async Task EnsureDepartmentExistsAsync(int deptId)
    {
        if (deptId <= 0)
            throw new ArgumentException("DeptId must be greater than zero.", nameof(deptId));

        var exists = await Db.Sys_Departments.AnyAsync(d => d.DeptId == deptId);
        if (!exists)
            throw new ArgumentException("Department does not exist.", nameof(deptId));
    }

    /// <summary>
    /// 取得指定部門與所有後代部門 ID，用於包含子部門的使用者查詢。
    /// </summary>
    private async Task<List<int>> GetDepartmentAndDescendantIdsAsync(int deptId)
    {
        await EnsureDepartmentExistsAsync(deptId);

        var departments = await Db.Sys_Departments
            .AsNoTracking()
            .Select(d => new { d.DeptId, d.ParentDeptId })
            .ToListAsync();

        var ids = new List<int> { deptId };
        var pending = new Queue<int>();
        pending.Enqueue(deptId);

        while (pending.Count > 0)
        {
            var parentDeptId = pending.Dequeue();
            foreach (var child in departments.Where(d => d.ParentDeptId == parentDeptId))
            {
                ids.Add(child.DeptId);
                pending.Enqueue(child.DeptId);
            }
        }

        return ids;
    }

    /// <summary>
    /// 將使用者查詢左關聯部門資料，投影成包含 DeptName 的 UserDto。
    /// </summary>
    private IQueryable<UserDto> ApplyDepartmentJoin(IQueryable<Sys_UserInfo> query)
    {
        return
            from user in query
            join department in Db.Sys_Departments.AsNoTracking()
                on user.DeptId equals department.DeptId into departments
            from department in departments.DefaultIfEmpty()
            select new UserDto
            {
                Id = user.Id,
                UserId = user.UserId,
                UserName = user.UserName ?? string.Empty,
                DeptId = user.DeptId,
                DeptName = department == null ? string.Empty : department.DeptName,
                MobilePhone = user.MobilePhone ?? string.Empty,
                Email = user.Email ?? string.Empty,
                LoginFailCount = user.LoginFailCount,
                IsEnable = user.IsEnable,
                LastLoginTime = user.LastLoginTime,
                LastLoginIp = user.LastLoginIp ?? string.Empty,
                CreatedTime = user.CreatedTime,
                UpdatedTime = user.UpdatedTime == default ? null : user.UpdatedTime
            };
    }

    /// <summary>
    /// 將使用者資料表實體轉成輸出 DTO；用於建立後的保底回傳。
    /// </summary>
    private static UserDto MapToDto(Sys_UserInfo user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserId = user.UserId,
            UserName = user.UserName ?? string.Empty,
            DeptId = user.DeptId,
            MobilePhone = user.MobilePhone ?? string.Empty,
            Email = user.Email ?? string.Empty,
            LoginFailCount = user.LoginFailCount,
            IsEnable = user.IsEnable,
            LastLoginTime = user.LastLoginTime,
            LastLoginIp = user.LastLoginIp ?? string.Empty,
            CreatedTime = user.CreatedTime,
            UpdatedTime = user.UpdatedTime == default ? null : user.UpdatedTime
        };
    }

}


