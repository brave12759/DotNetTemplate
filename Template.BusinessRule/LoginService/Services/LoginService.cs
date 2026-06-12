using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.PasswordManager.Services;
using Template.BusinessRule.TokenRevocationService.Services;
using Template.Common.Enums;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.LoginService.Services;

/// <summary>
/// 登入服務，負責帳密驗證、登入失敗鎖定、密碼過期、Token 產生與 Token 撤銷。
/// </summary>
public class LoginService(IServiceProvider serviceProvider) : BaseService(serviceProvider), ILoginService
{
    private static readonly string SystemSettingType = SystemSettingTypeEnum.SystemSetting.ToSettingTypeValue();
    private static readonly string LoginFailLimitKey = SystemSettingKeyEnum.LoginFailLimit.ToSettingKeyValue();
    private static readonly string AccountFailLockKey = SystemSettingKeyEnum.AccountFailLock.ToSettingKeyValue();
    private static readonly string PasswordExpireKey = SystemSettingKeyEnum.PasswordExpire.ToSettingKeyValue();

    private readonly Lazy<IPasswordManager> _passwordManager = new(() =>
        serviceProvider.GetRequiredService<IPasswordManager>());
    private readonly Lazy<IJwtService> _jwtService = new(() =>
        serviceProvider.GetRequiredService<IJwtService>());
    private readonly Lazy<ITokenRevocationService> _tokenRevocationService = new(() =>
        serviceProvider.GetRequiredService<ITokenRevocationService>());
    private readonly Lazy<ILogService> _logService = new(() =>
        serviceProvider.GetRequiredService<ILogService>());

    /// <inheritdoc />
    public async Task<LoginResult> LoginAsync(string userId, string password, string ip)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
        {
            await WriteLoginAuditAsync(
                AuditResultEnum.Failure,
                userId,
                ip,
                "登入失敗：帳號或密碼未填寫。",
                new { Reason = "MissingCredential" });
            return LoginResult.Fail("Invalid account or password.");
        }

        var normalizedUserId = userId.Trim();
        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.UserId == normalizedUserId);
        if (user is null)
        {
            await WriteLoginAuditAsync(
                AuditResultEnum.Failure,
                normalizedUserId,
                ip,
                "登入失敗：帳號不存在。",
                new { Reason = "UserNotFound" });
            return LoginResult.Fail("Invalid account or password.");
        }

        var now = DateTime.UtcNow;
        var loginFailLimit = await GetPositiveSettingIntAsync(LoginFailLimitKey);
        var accountFailLockMinutes = await GetPositiveSettingIntAsync(AccountFailLockKey);

        if (IsTemporaryLockoutActive(user, now, loginFailLimit, accountFailLockMinutes))
        {
            ExtendLockout(user, now);
            await Db.SaveChangesAsync();
            await WriteLoginAuditAsync(
                AuditResultEnum.Failure,
                user.UserId,
                ip,
                "登入失敗：帳號仍在鎖定期間，已延長鎖定起算時間。",
                new { Reason = "LockoutActive", user.LoginFailCount });
            return LoginResult.AccountLockedOut("Account is temporarily locked.");
        }

        if (IsTemporaryLockoutExpired(user, now, loginFailLimit, accountFailLockMinutes))
        {
            ClearLoginFailures(user, now);
            await Db.SaveChangesAsync();
        }

        if (!user.IsEnable)
        {
            await WriteLoginAuditAsync(
                AuditResultEnum.Failure,
                user.UserId,
                ip,
                "登入失敗：帳號已停用。",
                new { Reason = "AccountDisabled" });
            return LoginResult.Fail("Account is disabled.");
        }

        if (!_passwordManager.Value.Verify(password, user.Password))
        {
            user.LoginFailCount++;
            user.UpdatedTime = now;
            user.UpdatedId = "system";

            if (loginFailLimit > 0 && user.LoginFailCount >= loginFailLimit)
            {
                await Db.SaveChangesAsync();
                await WriteLoginAuditAsync(
                    AuditResultEnum.Failure,
                    user.UserId,
                    ip,
                    "登入失敗：密碼錯誤且已達鎖定門檻。",
                    new { Reason = "WrongPasswordLocked", user.LoginFailCount });
                return LoginResult.AccountLockedOut("Account is temporarily locked.");
            }

            await Db.SaveChangesAsync();
            await WriteLoginAuditAsync(
                AuditResultEnum.Failure,
                user.UserId,
                ip,
                "登入失敗：密碼錯誤。",
                new { Reason = "WrongPassword", user.LoginFailCount });
            return LoginResult.Fail("Invalid account or password.");
        }

        if (await IsPasswordExpiredAsync(user, now))
        {
            await WriteLoginAuditAsync(
                AuditResultEnum.Failure,
                user.UserId,
                ip,
                "登入失敗：密碼已過期。",
                new { Reason = "PasswordExpired" });
            return LoginResult.PasswordExpiredResult("Password has expired.");
        }

        user.LoginFailCount = 0;
        user.LastLoginTime = now;
        user.LastLoginIp = ip;
        user.UpdatedTime = now;
        user.UpdatedId = "system";
        await Db.SaveChangesAsync();

        var token = await _jwtService.Value.GeneratePersonalTokenAsync(
            userId: user.UserId,
            email: user.Email ?? string.Empty,
            mobilePhone: user.MobilePhone ?? string.Empty,
            deptId: user.DeptId.ToString(),
            ip: ip);

        await WriteLoginAuditAsync(
            AuditResultEnum.Success,
            user.UserId,
            ip,
            "登入成功。",
            new { Reason = "Success" });
        return LoginResult.Ok(token);
    }

    /// <inheritdoc />
    public async Task<LoginResult> DevLoginAsync(string userId, string ip)
    {
        var user = await Db.Sys_UserInfos.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId);

        var token = user is not null
            ? await _jwtService.Value.GeneratePersonalTokenAsync(
                userId: user.UserId,
                email: user.Email ?? string.Empty,
                mobilePhone: user.MobilePhone ?? string.Empty,
                deptId: user.DeptId.ToString(),
                ip: ip)
            : await _jwtService.Value.GeneratePersonalTokenAsync(
                userId: userId,
                email: "dev@localhost",
                mobilePhone: string.Empty,
                deptId: "0",
                ip: ip);

        await WriteUserOperationAuditAsync(
            AuditActionEnum.Login,
            AuditResultEnum.Success,
            userId,
            ip,
            "開發模式登入成功。",
            new { Reason = "DevLogin", UserExists = user is not null });
        return LoginResult.Ok(token);
    }

    /// <inheritdoc />
    public async Task<LoginResult> RefreshAsync(string userId, string tokenId, long expiredUnixTimeSeconds, string ip)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(tokenId) || expiredUnixTimeSeconds <= 0)
        {
            await WriteUserOperationAuditAsync(
                AuditActionEnum.RefreshToken,
                AuditResultEnum.Failure,
                userId,
                ip,
                "刷新 Token 失敗：Token 資料不完整。",
                new { Reason = "InvalidTokenPayload" });
            return LoginResult.Fail("Invalid token.");
        }

        var user = await Db.Sys_UserInfos.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user is null || !user.IsEnable)
        {
            await WriteUserOperationAuditAsync(
                AuditActionEnum.RefreshToken,
                AuditResultEnum.Failure,
                userId,
                ip,
                "刷新 Token 失敗：帳號不存在或已停用。",
                new { Reason = "AccountDisabledOrNotFound" });
            return LoginResult.Fail("Account is disabled.");
        }

        var now = DateTime.UtcNow;
        var loginFailLimit = await GetPositiveSettingIntAsync(LoginFailLimitKey);
        var accountFailLockMinutes = await GetPositiveSettingIntAsync(AccountFailLockKey);
        if (IsTemporaryLockoutActive(user, now, loginFailLimit, accountFailLockMinutes))
        {
            await WriteUserOperationAuditAsync(
                AuditActionEnum.RefreshToken,
                AuditResultEnum.Failure,
                user.UserId,
                ip,
                "刷新 Token 失敗：帳號仍在鎖定期間。",
                new { Reason = "LockoutActive", user.LoginFailCount });
            return LoginResult.AccountLockedOut("Account is temporarily locked.");
        }

        if (await IsPasswordExpiredAsync(user, now))
        {
            await WriteUserOperationAuditAsync(
                AuditActionEnum.RefreshToken,
                AuditResultEnum.Failure,
                user.UserId,
                ip,
                "刷新 Token 失敗：密碼已過期。",
                new { Reason = "PasswordExpired" });
            return LoginResult.PasswordExpiredResult("Password has expired.");
        }

        user.LastLoginTime = now;
        user.LastLoginIp = ip;
        user.UpdatedTime = now;
        user.UpdatedId = "system";
        await Db.SaveChangesAsync();

        var newToken = await _jwtService.Value.GeneratePersonalTokenAsync(
            userId: user.UserId,
            email: user.Email ?? string.Empty,
            mobilePhone: user.MobilePhone ?? string.Empty,
            deptId: user.DeptId.ToString(),
            ip: ip);

        _tokenRevocationService.Value.Revoke(tokenId, expiredUnixTimeSeconds);
        await WriteUserOperationAuditAsync(
            AuditActionEnum.RefreshToken,
            AuditResultEnum.Success,
            user.UserId,
            ip,
            "刷新 Token 成功，舊 Token 已撤銷。",
            new { Reason = "Success" });
        return LoginResult.Ok(newToken);
    }

    /// <inheritdoc />
    public async Task LogoutAsync(string tokenId, long expiredUnixTimeSeconds)
    {
        _tokenRevocationService.Value.Revoke(tokenId, expiredUnixTimeSeconds);
        await WriteUserOperationAuditAsync(
            AuditActionEnum.Logout,
            AuditResultEnum.Success,
            string.Empty,
            string.Empty,
            "登出成功，Token 已撤銷。",
            new { TokenId = tokenId, ExpiredUnixTimeSeconds = expiredUnixTimeSeconds });
    }

    /// <summary>
    /// 判斷帳號是否仍在登入失敗鎖定期間；鎖定起算點使用最後一次更新時間。
    /// </summary>
    private static bool IsTemporaryLockoutActive(
        Sys_UserInfo user,
        DateTime now,
        int loginFailLimit,
        int accountFailLockMinutes)
    {
        if (loginFailLimit <= 0 || user.LoginFailCount < loginFailLimit)
            return false;

        if (accountFailLockMinutes <= 0)
            return true;

        return GetLockoutStartTime(user).AddMinutes(accountFailLockMinutes) > now;
    }

    /// <summary>
    /// 判斷登入失敗鎖定是否已到期；到期後才會清除失敗次數。
    /// </summary>
    private static bool IsTemporaryLockoutExpired(
        Sys_UserInfo user,
        DateTime now,
        int loginFailLimit,
        int accountFailLockMinutes)
    {
        return loginFailLimit > 0 &&
               accountFailLockMinutes > 0 &&
               user.LoginFailCount >= loginFailLimit &&
               GetLockoutStartTime(user).AddMinutes(accountFailLockMinutes) <= now;
    }

    /// <summary>
    /// 鎖定期間再次登入時延長鎖定時間，讓鎖定期從這次嘗試重新起算。
    /// </summary>
    private static void ExtendLockout(Sys_UserInfo user, DateTime now)
    {
        user.UpdatedTime = now;
        user.UpdatedId = "system";
    }

    /// <summary>
    /// 清除登入失敗狀態；不變更帳號啟用狀態，避免把人工停用的帳號自動打開。
    /// </summary>
    private static void ClearLoginFailures(Sys_UserInfo user, DateTime now)
    {
        user.LoginFailCount = 0;
        user.UpdatedTime = now;
        user.UpdatedId = "system";
    }

    /// <summary>
    /// 取得鎖定起算時間；舊資料若沒有更新時間，退回建立時間避免計算失真。
    /// </summary>
    private static DateTime GetLockoutStartTime(Sys_UserInfo user)
    {
        return user.UpdatedTime == default ? user.CreatedTime : user.UpdatedTime;
    }

    /// <summary>
    /// 檢查密碼是否超過系統設定的有效天數。
    /// </summary>
    private async Task<bool> IsPasswordExpiredAsync(Sys_UserInfo user, DateTime now)
    {
        var passwordExpireDays = await GetPositiveSettingIntAsync(PasswordExpireKey);
        if (passwordExpireDays <= 0)
            return false;

        var passwordChangedTime = await Db.Sys_UserPasswordHistories
            .AsNoTracking()
            .Where(h => h.UserId == user.UserId)
            .OrderByDescending(h => h.ChangedTime)
            .ThenByDescending(h => h.Id)
            .Select(h => (DateTime?)h.ChangedTime)
            .FirstOrDefaultAsync() ?? user.CreatedTime;

        return passwordChangedTime.AddDays(passwordExpireDays) <= now;
    }

    /// <summary>
    /// 從 Sys_BasicSettings 讀取正整數設定；缺少或格式錯誤時視為 0。
    /// </summary>
    private async Task<int> GetPositiveSettingIntAsync(string key)
    {
        var setting = await Db.Sys_BasicSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Type == SystemSettingType && s.Key == key);

        return int.TryParse(setting?.Value, out var value) && value > 0 ? value : 0;
    }

    /// <summary>
    /// 寫入登入事件日誌；登入流程固定使用 Login 操作種類。
    /// </summary>
    private Task WriteLoginAuditAsync(
        AuditResultEnum result,
        string userId,
        string ip,
        string message,
        object metadata)
    {
        return WriteUserOperationAuditAsync(AuditActionEnum.Login, result, userId, ip, message, metadata);
    }

    /// <summary>
    /// 寫入登入服務的使用者操作日誌；只記錄帳號、IP 與原因，不記錄密碼或完整 Token。
    /// </summary>
    private Task WriteUserOperationAuditAsync(
        AuditActionEnum action,
        AuditResultEnum result,
        string userId,
        string ip,
        string message,
        object metadata)
    {
        return _logService.Value.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "Login",
            Action = action,
            Result = result,
            UserId = userId,
            TargetType = "UserId",
            TargetId = userId.Trim(),
            IpAddress = ip.Trim(),
            Message = message,
            Metadata = metadata
        });
    }
}
