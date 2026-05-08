using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.Common.Models.User;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.UserService.Services;

/// <summary>
/// 使用者管理服務實作。
/// </summary>
public class UserService(IServiceProvider serviceProvider) : BaseService(serviceProvider), IUserService
{
    private readonly Lazy<IPasswordManager> _passwordManager = new(() =>
        serviceProvider.GetRequiredService<IPasswordManager>());

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserDto>> GetListAsync(string? keyword, bool? isEnable)
    {
        var query = Db.Sys_UserInfos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(u =>
                u.UserId.Contains(k) ||
                u.UserName.Contains(k) ||
                (u.Email ?? string.Empty).Contains(k) ||
                (u.MobilePhone ?? string.Empty).Contains(k) ||
                (u.DeptId ?? string.Empty).Contains(k));
        }

        if (isEnable.HasValue)
            query = query.Where(u => u.IsEnable == isEnable.Value);

        var users = await query
            .OrderBy(u => u.Id)
            .Select(ToDtoExpression())
            .ToListAsync();

        return users;
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id 必須大於 0。", nameof(id));

        return await Db.Sys_UserInfos
            .AsNoTracking()
            .Where(u => u.Id == id)
            .Select(ToDtoExpression())
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateAsync(UserCreateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("UserId 不可為空。", nameof(request.UserId));

        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new ArgumentException("UserName 不可為空。", nameof(request.UserName));

        var userId = request.UserId.Trim();

        var exists = await Db.Sys_UserInfos.AnyAsync(u => u.UserId == userId);
        if (exists)
            throw new ArgumentException("UserId 已存在，請使用其他帳號。", nameof(request.UserId));

        // 密碼為空時自動套用系統預設密碼
        var password = string.IsNullOrWhiteSpace(request.Password)
            ? await GetDefaultPasswordAsync()
            : request.Password;

        var now = DateTime.UtcNow;
        var createdBy = CurrentUserService?.CurrentUser?.UserId ?? "system";
        var entity = new Sys_UserInfo
        {
            UserId = userId,
            UserName = request.UserName.Trim(),
            Password = _passwordManager.Value.HashForStorage(password),
            DeptId = request.DeptId.Trim(),
            MobilePhone = request.MobilePhone.Trim(),
            Email = request.Email.Trim(),
            IsEnable = request.IsEnable,
            LoginFailCount = 0,
            LastLoginIp = null,
            LastLoginTime = null,
            CreatedTime = now,
            CreatedId = createdBy,
            UpdatedTime = now,
            UpdatedId = createdBy,
        };

        Db.Sys_UserInfos.Add(entity);
        await Db.SaveChangesAsync();

        return MapToDto(entity);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateAsync(UserUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id <= 0)
            throw new ArgumentException("Id 必須大於 0。", nameof(request.Id));

        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new ArgumentException("UserName 不可為空。", nameof(request.UserName));

        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.Id == request.Id);
        if (user is null)
            return false;

        user.UserName = request.UserName.Trim();
        user.DeptId = request.DeptId.Trim();
        user.MobilePhone = request.MobilePhone.Trim();
        user.Email = request.Email.Trim();
        user.IsEnable = request.IsEnable;
        user.UpdatedTime = DateTime.UtcNow;
        user.UpdatedId = CurrentUserService?.CurrentUser?.UserId ?? "system";

        await Db.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentException("Id 必須大於 0。", nameof(id));

        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.Id == id);
        if (user is null)
            return false;

        Db.Sys_UserInfos.Remove(user);
        await Db.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordAsync(UserResetPasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id <= 0)
            throw new ArgumentException("Id 必須大於 0。", nameof(request.Id));

        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.Id == request.Id);
        if (user is null)
            return false;

        // NewPassword 為空時使用系統預設密碼
        var newPassword = string.IsNullOrWhiteSpace(request.NewPassword)
            ? await GetDefaultPasswordAsync()
            : request.NewPassword;

        user.Password = _passwordManager.Value.HashForStorage(newPassword);
        user.LoginFailCount = 0;
        user.IsEnable = true; // 管理員重設密碼同時恢復啟用
        user.UpdatedTime = DateTime.UtcNow;
        user.UpdatedId = CurrentUserService?.CurrentUser?.UserId ?? "system";

        await Db.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(UserChangePasswordRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Id <= 0)
            throw new ArgumentException("Id 必須大於 0。", nameof(request.Id));

        if (string.IsNullOrWhiteSpace(request.OldPassword))
            throw new ArgumentException("舊密碼不可為空。", nameof(request.OldPassword));

        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.Id == request.Id);
        if (user is null)
            return false;

        if (!_passwordManager.Value.Verify(request.OldPassword, user.Password))
            throw new UnauthorizedAccessException("舊密碼驗證失敗。");

        _passwordManager.Value.ValidateNewPassword(request.NewPassword);

        user.Password = _passwordManager.Value.HashForStorage(request.NewPassword);
        user.UpdatedTime = DateTime.UtcNow;
        user.UpdatedId = CurrentUserService?.CurrentUser?.UserId ?? "system";

        await Db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// 從 Sys_BasicSettings 讀取系統預設密碼。
    /// 未設定時丟出 <see cref="InvalidOperationException"/>。
    /// </summary>
    private async Task<string> GetDefaultPasswordAsync()
    {
        var setting = await Db.Sys_BasicSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Type == "SystemSetting" && s.Key == "DefaultPassword");

        if (string.IsNullOrWhiteSpace(setting?.Value))
            throw new InvalidOperationException(
                "系統未設定預設密碼，請在 Sys_BasicSettings 中新增 Type=SystemSetting、Key=DefaultPassword 的記錄。");

        return setting.Value;
    }

    private static UserDto MapToDto(Sys_UserInfo user)
    {
        return new UserDto
        {
            Id = user.Id,
            UserId = user.UserId,
            UserName = user.UserName ?? string.Empty,
            DeptId = user.DeptId ?? string.Empty,
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

    private static System.Linq.Expressions.Expression<Func<Sys_UserInfo, UserDto>> ToDtoExpression()
    {
        return user => new UserDto
        {
            Id = user.Id,
            UserId = user.UserId,
            UserName = user.UserName ?? string.Empty,
            DeptId = user.DeptId ?? string.Empty,
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
