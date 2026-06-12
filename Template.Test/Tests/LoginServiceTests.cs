using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.LoginService.Services;
using Template.BusinessRule.CryptographyService.Services;
using Template.BusinessRule.PasswordManager.Services;
using Template.BusinessRule.TokenRevocationService.Services;
using Template.Common.Enums;
using Template.Common.Models.Jwt;
using Template.Common.Services;
using Template.DataAccess;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class LoginServiceTests
{
    // ????????????????????????????????????????????????????????????????????????????????????????????
    // ????擗
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task LoginAsync_CorrectCredentials_Should_ReturnOk_WithToken()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("alice", pm.HashForStorage("ValidPass@123"), isEnable: true, loginFailCount: 0));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("alice", "ValidPass@123", "127.0.0.1");

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(result.Token));
        Assert.IsFalse(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "alice");
        Assert.AreEqual(0, entity.LoginFailCount);
        Assert.IsNotNull(entity.LastLoginTime);
        Assert.AreEqual("127.0.0.1", entity.LastLoginIp);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        AssertSingleUserOperationLog(logService, AuditActionEnum.Login, AuditResultEnum.Success, "alice");
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // ????????
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task LoginAsync_UserNotFound_Should_ReturnFail()
    {
        using var scope = BuildScope();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("nobody", "anything", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);
        Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        AssertSingleUserOperationLog(logService, AuditActionEnum.Login, AuditResultEnum.Failure, "nobody");
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // ?????芰??????謅暑??
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task LoginAsync_WrongPassword_Should_ReturnFail_AndIncrementFailCount()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("bob", pm.HashForStorage("CorrectPass@123"), isEnable: true, loginFailCount: 0));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("bob", "WrongPass", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "bob");
        Assert.AreEqual(1, entity.LoginFailCount);
        Assert.IsTrue(entity.IsEnable);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        AssertSingleUserOperationLog(logService, AuditActionEnum.Login, AuditResultEnum.Failure, "bob");
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // ?????芰???????????謚秋????
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task LoginAsync_WrongPassword_ReachLimit_Should_ReturnLockedOut_WithoutDisablingAccount()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        // ?剜????脫??? limit-1???????仿鞎赤?謚秋?
        db.Sys_UserInfos.Add(MakeUser("charlie", pm.HashForStorage("CorrectPass@123"), isEnable: true, loginFailCount: 4));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("charlie", "WrongPass", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "charlie");
        Assert.AreEqual(5, entity.LoginFailCount);
        Assert.IsTrue(entity.IsEnable);
        Assert.IsTrue(entity.UpdatedTime > entity.CreatedTime);
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // ?????????剜????脤??? ??AccountLockedOut
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task LoginAsync_DisabledAccount_WithActiveLockout_Should_ReturnLockedOut()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        var user = MakeUser("dave", pm.HashForStorage("Password@123"), isEnable: false, loginFailCount: 5);
        user.UpdatedTime = DateTime.UtcNow.AddMinutes(-5);
        db.Sys_UserInfos.Add(user);
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        db.Sys_BasicSettings.Add(MakeSetting("AccountFailLock", "15"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("dave", "Password@123", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.AccountDisabled);
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // ?????????豯??剜????????鈭?????????Fail
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task LoginAsync_DisabledAccount_ManuallyDisabled_Should_ReturnFail()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("eve", pm.HashForStorage("Password@123"), isEnable: false, loginFailCount: 0));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("eve", "Password@123", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // LoginFailLimit ?桀?蹌?0 ?????????謅暑??
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task LoginAsync_WrongPassword_NoLimit_Should_NeverLockOut()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("frank", pm.HashForStorage("CorrectPass@123"), isEnable: true, loginFailCount: 99));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "0"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("frank", "WrongPass", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "frank");
        Assert.IsTrue(entity.IsEnable);
    }

    [TestMethod]
    public async Task LoginAsync_WrongPassword_MissingLimit_Should_NeverLockOut()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("irene", pm.HashForStorage("CorrectPass@123"), isEnable: true, loginFailCount: 99));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("irene", "WrongPass", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "irene");
        Assert.IsTrue(entity.IsEnable);
        Assert.AreEqual(100, entity.LoginFailCount);
    }

    [TestMethod]
    public async Task LoginAsync_WrongPassword_NonnumericLimit_Should_NeverLockOut()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("jane", pm.HashForStorage("CorrectPass@123"), isEnable: true, loginFailCount: 99));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "not-a-number"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("jane", "WrongPass", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "jane");
        Assert.IsTrue(entity.IsEnable);
        Assert.AreEqual(100, entity.LoginFailCount);
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // ?擗????綽???剜????脤????
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task LoginAsync_Success_After_PreviousFailures_Should_ClearFailCount()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("grace", pm.HashForStorage("CorrectPass@123"), isEnable: true, loginFailCount: 3));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("grace", "CorrectPass@123", "10.0.0.1");

        Assert.IsTrue(result.Success);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "grace");
        Assert.AreEqual(0, entity.LoginFailCount);
        Assert.AreEqual("10.0.0.1", entity.LastLoginIp);
    }

    [TestMethod]
    public async Task LoginAsync_DisabledAccount_LockoutExpired_Should_ClearFailCountButRemainDisabled()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        var user = MakeUser("lock-expired", pm.HashForStorage("CorrectPass@123"), isEnable: false, loginFailCount: 5);
        user.UpdatedTime = DateTime.UtcNow.AddMinutes(-20);
        db.Sys_UserInfos.Add(user);
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        db.Sys_BasicSettings.Add(MakeSetting("AccountFailLock", "15"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("lock-expired", "CorrectPass@123", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "lock-expired");
        Assert.IsFalse(entity.IsEnable);
        Assert.AreEqual(0, entity.LoginFailCount);
    }

    [TestMethod]
    public async Task LoginAsync_ActiveLockout_Should_ExtendLockoutByUpdatingUpdatedTime()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        var user = MakeUser("locked", pm.HashForStorage("CorrectPass@123"), isEnable: true, loginFailCount: 5);
        user.UpdatedTime = DateTime.UtcNow.AddMinutes(-5);
        db.Sys_UserInfos.Add(user);
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        db.Sys_BasicSettings.Add(MakeSetting("AccountFailLock", "15"));
        await db.SaveChangesAsync();

        var previousUpdatedTime = user.UpdatedTime;
        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("locked", "CorrectPass@123", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "locked");
        Assert.AreEqual(5, entity.LoginFailCount);
        Assert.IsTrue(entity.UpdatedTime > previousUpdatedTime);
    }

    [TestMethod]
    public async Task LoginAsync_PasswordExpired_Should_ReturnPasswordExpired()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        var user = MakeUser("expired", pm.HashForStorage("CorrectPass@123"), isEnable: true, loginFailCount: 0);
        db.Sys_UserInfos.Add(user);
        db.Sys_UserPasswordHistories.Add(MakePasswordHistory(user.UserId, user.Password, DateTime.UtcNow.AddDays(-181)));
        db.Sys_BasicSettings.Add(MakeSetting("PassWordExpire", "180"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("expired", "CorrectPass@123", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.PasswordExpired);
        Assert.IsTrue(string.IsNullOrEmpty(result.Token));
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // DevLoginAsync?垢???鄞????
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task DevLoginAsync_ExistingUser_Should_ReturnOk_WithToken()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("henry", pm.HashForStorage("Irrelevant@123"), isEnable: true, loginFailCount: 0));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.DevLoginAsync("henry", "127.0.0.1");

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(result.Token));
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // DevLoginAsync?垢???鄞???殉朱謓????嚗??頦? token
    // ????????????????????????????????????????????????????????????????????????????????????????????

    [TestMethod]
    public async Task DevLoginAsync_NonExistingUser_Should_ReturnOk_WithAnonymousToken()
    {
        using var scope = BuildScope();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.DevLoginAsync("ghost", "127.0.0.1");

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(result.Token));
    }

    [TestMethod]
    public async Task LogoutAsync_Should_RevokeToken()
    {
        using var scope = BuildScope();
        var revocation = scope.ServiceProvider.GetRequiredService<RecordingTokenRevocationService>();
        var sut = new LoginService(scope.ServiceProvider);

        await sut.LogoutAsync("token-1", 1234567890);

        Assert.AreEqual("token-1", revocation.TokenId);
        Assert.AreEqual(1234567890, revocation.ExpiredUnixTimeSeconds);
        Assert.IsTrue(revocation.IsRevoked("token-1"));
    }

    [TestMethod]
    public async Task RefreshAsync_ValidUser_Should_ReturnNewToken_AndRevokeOldToken()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();
        var revocation = scope.ServiceProvider.GetRequiredService<RecordingTokenRevocationService>();

        db.Sys_UserInfos.Add(MakeUser("refresh-user", pm.HashForStorage("ValidPass@123"), isEnable: true, loginFailCount: 0));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.RefreshAsync("refresh-user", "old-token-id", 1234567890, "127.0.0.1");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("fake-jwt-token-for-refresh-user", result.Token);
        Assert.AreEqual("old-token-id", revocation.TokenId);
        Assert.AreEqual(1234567890, revocation.ExpiredUnixTimeSeconds);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        AssertSingleUserOperationLog(logService, AuditActionEnum.RefreshToken, AuditResultEnum.Success, "refresh-user");
    }

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // ????撖?
    // ????????????????????????????????????????????????????????????????????????????????????????????

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"login-service-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICryptographyService, FakeHashOnlyCryptographyService>();
        services.AddScoped<IPasswordManager, PasswordManager>();
        services.AddSingleton<RecordingTokenRevocationService>();
        services.AddSingleton<ITokenRevocationService>(sp => sp.GetRequiredService<RecordingTokenRevocationService>());
        services.AddScoped<IJwtService, FakeJwtService>();
        services.AddSingleton<RecordingLogService>();
        services.AddScoped<ILogService>(sp => sp.GetRequiredService<RecordingLogService>());

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private static Sys_UserInfo MakeUser(string userId, string passwordHash, bool isEnable, int loginFailCount) =>
        new()
        {
            UserId = userId,
            UserName = userId,
            Password = passwordHash,
            DeptId = 1,
            MobilePhone = "0911111111",
            Email = $"{userId}@example.com",
            IsEnable = isEnable,
            LoginFailCount = loginFailCount,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "admin"
        };

    private static Sys_UserPasswordHistory MakePasswordHistory(string userId, string passwordHash, DateTime changedTime) =>
        new()
        {
            UserId = userId,
            PasswordHash = passwordHash,
            ChangeType = 1,
            ChangedTime = changedTime,
            ChangedId = "admin"
        };

    private static Sys_BasicSetting MakeSetting(string key, string value) =>
        new()
        {
            Type = "SystemSetting",
            Key = key,
            Value = value,
            Label = key,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin"
        };

    // ????????????????????????????????????????????????????????????????????????????????????????????
    // Fake ??
    // ????????????????????????????????????????????????????????????????????????????????????????????

    private sealed class FakeHashOnlyCryptographyService : ICryptographyService
    {
        public (string KeyBase64, string IvBase64) GenerateSymmetricKey(int keySizeBits = 256) => throw new NotImplementedException();
        public string SymmetricEncrypt(string plainText) => throw new NotImplementedException();
        public string SymmetricDecrypt(string cipherTextBase64) => throw new NotImplementedException();
        public (string PublicKeyPem, string PrivateKeyPem) GenerateRsaKeyPair(int keySizeBits = 2048) => throw new NotImplementedException();
        public string AsymmetricEncrypt(string plainText) => throw new NotImplementedException();
        public string AsymmetricDecrypt(string cipherTextBase64) => throw new NotImplementedException();
        public string Sign(string plainText) => throw new NotImplementedException();
        public bool VerifySignature(string plainText, string signatureBase64) => throw new NotImplementedException();
        public string Hash(string plainText) => $"HASH::{plainText}";
        public bool VerifyHash(string plainText, string hashValue) => hashValue == Hash(plainText);
    }

    private sealed class FakeJwtService : IJwtService
    {
        public Task<string> GeneratePersonalTokenAsync(
            string userId,
            string email,
            string mobilePhone,
            string deptId,
            string ip,
            string? roleGroupsJson = null,
            string? functionPermissionsJson = null) =>
            Task.FromResult($"fake-jwt-token-for-{userId}");

        public Task<string> GenerateServerTokenAsync(string clientId, string ip) =>
            Task.FromResult($"fake-server-token-for-{clientId}");

        public Task<System.Security.Claims.ClaimsPrincipal?> ValidateTokenAsync(string token, bool validateRevocation = true) =>
            Task.FromResult<System.Security.Claims.ClaimsPrincipal?>(null);

        public Task<JwtSettingDto> GetSettingsAsync() => throw new NotImplementedException();

        public Task UpdateSettingsAsync(JwtSettingUpdateRequest request, string updatedBy) => throw new NotImplementedException();
    }

    private sealed class RecordingTokenRevocationService : ITokenRevocationService
    {
        public string? TokenId { get; private set; }
        public long ExpiredUnixTimeSeconds { get; private set; }

        public void Revoke(string tokenId, long expiredUnixTimeSeconds)
        {
            TokenId = tokenId;
            ExpiredUnixTimeSeconds = expiredUnixTimeSeconds;
        }

        public bool IsRevoked(string tokenId) => TokenId == tokenId;
    }

    private static UserOperationLogCreateRequest AssertSingleUserOperationLog(
        RecordingLogService logService,
        AuditActionEnum action,
        AuditResultEnum result,
        string targetUserId)
    {
        var logs = logService.UserOperations
            .Where(x => x.Action == action && x.Result == result && x.TargetId == targetUserId)
            .ToList();

        Assert.AreEqual(1, logs.Count);
        return logs[0];
    }

    private sealed class RecordingLogService : ILogService
    {
        public List<UserOperationLogCreateRequest> UserOperations { get; } = [];

        public Task<long> WriteUserOperationAsync(
            UserOperationLogCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            UserOperations.Add(request);
            return Task.FromResult((long)UserOperations.Count);
        }

        public Task<UserOperationLogQueryResult> GetUserOperationLogsAsync(
            UserOperationLogQueryRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<long> WriteQueueAsync(QueueLogCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<QueueLogQueryResult> GetQueueLogsAsync(QueueLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<long> WriteSsoAsync(SsoLogCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<SsoLogQueryResult> GetSsoLogsAsync(SsoLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

    }
}
