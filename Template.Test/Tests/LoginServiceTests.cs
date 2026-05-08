using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.LoginService.Services;
using Template.BusinessRule.PasswordManager.Services;
using Template.BusinessRule.TokenRevocationService.Services;
using Template.Common.Services;
using Template.DataAccess;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class LoginServiceTests
{
    // ──────────────────────────────────────────────
    // 成功登入
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task LoginAsync_CorrectCredentials_Should_ReturnOk_WithToken()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("alice", pm.HashForStorage("P@ssword1"), isEnable: true, loginFailCount: 0));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("alice", "P@ssword1", "127.0.0.1");

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(result.Token));
        Assert.IsFalse(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "alice");
        Assert.AreEqual(0, entity.LoginFailCount);
        Assert.IsNotNull(entity.LastLoginTime);
        Assert.AreEqual("127.0.0.1", entity.LastLoginIp);
    }

    // ──────────────────────────────────────────────
    // 帳號不存在
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task LoginAsync_UserNotFound_Should_ReturnFail()
    {
        using var scope = BuildScope();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("nobody", "anything", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);
        Assert.IsFalse(string.IsNullOrEmpty(result.ErrorMessage));
    }

    // ──────────────────────────────────────────────
    // 密碼錯誤，累計失敗次數
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task LoginAsync_WrongPassword_Should_ReturnFail_AndIncrementFailCount()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("bob", pm.HashForStorage("CorrectPass1"), isEnable: true, loginFailCount: 0));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("bob", "WrongPass", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "bob");
        Assert.AreEqual(1, entity.LoginFailCount);
        Assert.IsTrue(entity.IsEnable);
    }

    // ──────────────────────────────────────────────
    // 密碼錯誤達上限，自動停用帳號
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task LoginAsync_WrongPassword_ReachLimit_Should_ReturnLockedOut_AndDisableAccount()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        // 失敗次數已達 limit-1，再錯一次即觸發停用
        db.Sys_UserInfos.Add(MakeUser("charlie", pm.HashForStorage("CorrectPass1"), isEnable: true, loginFailCount: 4));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("charlie", "WrongPass", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "charlie");
        Assert.AreEqual(5, entity.LoginFailCount);
        Assert.IsFalse(entity.IsEnable);
    }

    // ──────────────────────────────────────────────
    // 帳號已停用且失敗次數超限 → AccountLockedOut
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task LoginAsync_DisabledAccount_WithHighFailCount_Should_ReturnLockedOut()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("dave", pm.HashForStorage("Password1"), isEnable: false, loginFailCount: 5));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("dave", "Password1", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.AccountDisabled);
    }

    // ──────────────────────────────────────────────
    // 帳號已停用但非因失敗超限（人工停用）→ 一般 Fail
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task LoginAsync_DisabledAccount_ManuallyDisabled_Should_ReturnFail()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("eve", pm.HashForStorage("Password1"), isEnable: false, loginFailCount: 0));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("eve", "Password1", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);
    }

    // ──────────────────────────────────────────────
    // LoginFailLimit 設為 0 → 不限制失敗次數
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task LoginAsync_WrongPassword_NoLimit_Should_NeverLockOut()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("frank", pm.HashForStorage("CorrectPass1"), isEnable: true, loginFailCount: 99));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "0"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("frank", "WrongPass", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.IsFalse(result.AccountDisabled);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "frank");
        Assert.IsTrue(entity.IsEnable);
    }

    // ──────────────────────────────────────────────
    // 登入成功後，失敗次數被清零
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task LoginAsync_Success_After_PreviousFailures_Should_ClearFailCount()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("grace", pm.HashForStorage("CorrectPass1"), isEnable: true, loginFailCount: 3));
        db.Sys_BasicSettings.Add(MakeSetting("LoginFailLimit", "5"));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.LoginAsync("grace", "CorrectPass1", "10.0.0.1");

        Assert.IsTrue(result.Success);

        var entity = await db.Sys_UserInfos.FirstAsync(u => u.UserId == "grace");
        Assert.AreEqual(0, entity.LoginFailCount);
        Assert.AreEqual("10.0.0.1", entity.LastLoginIp);
    }

    // ──────────────────────────────────────────────
    // DevLoginAsync：使用者存在
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task DevLoginAsync_ExistingUser_Should_ReturnOk_WithToken()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var pm = scope.ServiceProvider.GetRequiredService<IPasswordManager>();

        db.Sys_UserInfos.Add(MakeUser("henry", pm.HashForStorage("Irrelevant1"), isEnable: true, loginFailCount: 0));
        await db.SaveChangesAsync();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.DevLoginAsync("henry", "127.0.0.1");

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(result.Token));
    }

    // ──────────────────────────────────────────────
    // DevLoginAsync：使用者不存在 → 產生匿名 token
    // ──────────────────────────────────────────────

    [TestMethod]
    public async Task DevLoginAsync_NonExistingUser_Should_ReturnOk_WithAnonymousToken()
    {
        using var scope = BuildScope();

        var sut = new LoginService(scope.ServiceProvider);
        var result = await sut.DevLoginAsync("ghost", "127.0.0.1");

        Assert.IsTrue(result.Success);
        Assert.IsFalse(string.IsNullOrEmpty(result.Token));
    }

    // ──────────────────────────────────────────────
    // 輔助方法
    // ──────────────────────────────────────────────

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"login-service-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICryptographyService, FakeHashOnlyCryptographyService>();
        services.AddScoped<IPasswordManager, PasswordManager>();
        services.AddSingleton<ITokenRevocationService, InMemoryTokenRevocationService>();
        services.AddScoped<IJwtService, FakeJwtService>();

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private static Sys_UserInfo MakeUser(string userId, string passwordHash, bool isEnable, int loginFailCount) =>
        new()
        {
            UserId = userId,
            UserName = userId,
            Password = passwordHash,
            DeptId = "IT",
            MobilePhone = "0911111111",
            Email = $"{userId}@example.com",
            IsEnable = isEnable,
            LoginFailCount = loginFailCount,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "admin"
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

    // ──────────────────────────────────────────────
    // Fake 實作
    // ──────────────────────────────────────────────

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
        public string GenerateToken(string userId, string email, string mobilePhone, string deptId, string ip) =>
            $"fake-jwt-token-for-{userId}";
    }
}
