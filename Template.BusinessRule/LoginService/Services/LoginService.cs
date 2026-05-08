using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.LoginService.Services;

/// <summary>
/// 登入服務實作。
/// 驗證帳號密碼並發放 JWT Token；提供開發假登入功能。
/// </summary>
/// <remarks>
/// 建立登入服務。
/// </remarks>
/// <param name="serviceProvider">DI 服務提供者。</param>
public class LoginService(IServiceProvider serviceProvider) : BaseService(serviceProvider), ILoginService
{
    private readonly Lazy<IPasswordManager> _passwordManager = new(() =>
        serviceProvider.GetRequiredService<IPasswordManager>());
    private readonly Lazy<IJwtService> _jwtService = new(() =>
        serviceProvider.GetRequiredService<IJwtService>());
    private readonly Lazy<ITokenRevocationService> _tokenRevocationService = new(() =>
        serviceProvider.GetRequiredService<ITokenRevocationService>());

    /// <inheritdoc />
    public async Task<LoginResult> LoginAsync(string userId, string password, string ip)
    {
        // 不加 AsNoTracking，因為成功/失敗都需要更新欄位
        var user = await Db.Sys_UserInfos
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user is null)
            return LoginResult.Fail("帳號不存在或已停用。");

        // 帳號已停用：判斷是否因登入失敗超限
        if (!user.IsEnable)
        {
            var limit = await GetLoginFailLimitAsync();
            if (limit > 0 && user.LoginFailCount >= limit)
                return LoginResult.AccountLockedOut("帳號多次登入失敗已被停用，請聯絡管理員。");

            return LoginResult.Fail("帳號不存在或已停用。");
        }

        // 密碼錯誤：計數並判斷是否超限
        if (!_passwordManager.Value.Verify(password, user.Password))
        {
            var limit = await GetLoginFailLimitAsync();
            user.LoginFailCount++;

            if (limit > 0 && user.LoginFailCount >= limit)
            {
                user.IsEnable = false;
                await Db.SaveChangesAsync();
                return LoginResult.AccountLockedOut("帳號多次登入失敗已被停用，請聯絡管理員。");
            }

            await Db.SaveChangesAsync();
            return LoginResult.Fail("帳號或密碼錯誤。");
        }

        // 登入成功：清除失敗計數、更新最後登入資訊
        user.LoginFailCount = 0;
        user.LastLoginTime = DateTime.UtcNow;
        user.LastLoginIp = ip;
        await Db.SaveChangesAsync();

        var token = _jwtService.Value.GenerateToken(
            userId:      user.UserId,
            email:       user.Email        ?? string.Empty,
            mobilePhone: user.MobilePhone  ?? string.Empty,
            deptId:      user.DeptId       ?? string.Empty,
            ip:          ip);

        return LoginResult.Ok(token);
    }

    /// <inheritdoc />
    public async Task<LoginResult> DevLoginAsync(string userId, string ip)
    {
        var user = await Db.Sys_UserInfos.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);

        var token = user is not null
            ? _jwtService.Value.GenerateToken(
                userId:      user.UserId,
                email:       user.Email        ?? string.Empty,
                mobilePhone: user.MobilePhone  ?? string.Empty,
                deptId:      user.DeptId       ?? string.Empty,
                ip:          ip)
            : _jwtService.Value.GenerateToken(
                userId:      userId,
                email:       "dev@localhost",
                mobilePhone: string.Empty,
                deptId:      "0",
                ip:          ip);

        return LoginResult.Ok(token);
    }

    /// <inheritdoc />
    public Task LogoutAsync(string tokenId, long expiredUnixTimeSeconds)
    {
        _tokenRevocationService.Value.Revoke(tokenId, expiredUnixTimeSeconds);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 從 Sys_BasicSettings 讀取最大登入失敗次數限制。
    /// 未設定時回傳 0（不限制）。
    /// </summary>
    private async Task<int> GetLoginFailLimitAsync()
    {
        var setting = await Db.Sys_BasicSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Type == "SystemSetting" && s.Key == "LoginFailLimit");

        return int.TryParse(setting?.Value, out var limit) ? limit : 0;
    }
}
