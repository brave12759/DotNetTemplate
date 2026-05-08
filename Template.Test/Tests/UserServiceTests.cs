using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.PasswordManager.Services;
using Template.BusinessRule.UserService.Services;
using Template.Common.Models;
using Template.Common.Models.User;
using Template.Common.Services;
using Template.DataAccess;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class UserServiceTests
{
    [TestMethod]
    public async Task CreateAsync_Should_CreateUser_AndHashPassword()
    {
        using var scope = BuildScope();
        var sut = new UserService(scope.ServiceProvider);

        var created = await sut.CreateAsync(new UserCreateRequest
        {
            UserId = "  alice  ",
            UserName = "Alice User",
            Password = "P@ssw0rd",
            DeptId = "IT",
            MobilePhone = "0911222333",
            Email = "alice@example.com",
            IsEnable = true
        });

        Assert.IsTrue(created.Id > 0);
        Assert.AreEqual("alice", created.UserId);
        Assert.AreEqual("Alice User", created.UserName);
        Assert.AreEqual("IT", created.DeptId);

        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var entity = await db.Sys_UserInfos.FirstAsync(x => x.Id == created.Id);
        Assert.AreEqual("HASH::P@ssw0rd", entity.Password);
    }

    [TestMethod]
    public async Task CreateAsync_DuplicateUserId_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sys_UserInfos.Add(new Sys_UserInfo
        {
            UserId = "bob",
            UserName = "Bob User",
            Password = "HASH::123",
            DeptId = "IT",
            MobilePhone = "0911111111",
            Email = "bob@example.com",
            IsEnable = true,
            LoginFailCount = 0,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "admin"
        });
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.CreateAsync(new UserCreateRequest
            {
                UserId = "bob",
                UserName = "Bob",
                Password = "another"
            }));
    }

    [TestMethod]
    public async Task GetListAsync_Should_Filter_ByKeyword_AndStatus()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        db.Sys_UserInfos.AddRange(
            new Sys_UserInfo
            {
                UserId = "userA",
                UserName = "User A",
                Email = "a@example.com",
                DeptId = "HR",
                MobilePhone = "0911111111",
                Password = "HASH::A",
                IsEnable = true,
                LoginFailCount = 0,
                CreatedTime = DateTime.UtcNow,
                CreatedId = "admin",
                UpdatedTime = DateTime.UtcNow,
                UpdatedId = "admin"
            },
            new Sys_UserInfo
            {
                UserId = "userB",
                UserName = "User B",
                Email = "b@example.com",
                DeptId = "IT",
                MobilePhone = "0922222222",
                Password = "HASH::B",
                IsEnable = false,
                LoginFailCount = 0,
                CreatedTime = DateTime.UtcNow,
                CreatedId = "admin",
                UpdatedTime = DateTime.UtcNow,
                UpdatedId = "admin"
            });

        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);

        var byKeyword = await sut.GetListAsync("example.com", null);
        var byStatus = await sut.GetListAsync(null, true);

        Assert.AreEqual(2, byKeyword.Count);
        Assert.AreEqual(1, byStatus.Count);
        Assert.AreEqual("userA", byStatus[0].UserId);
    }

    [TestMethod]
    public async Task UpdateAsync_ExistingUser_Should_ReturnTrue_AndPersist()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var user = new Sys_UserInfo
        {
            UserId = "charlie",
            UserName = "Charlie User",
            Password = "HASH::C",
            DeptId = "HR",
            MobilePhone = "0911111111",
            Email = "charlie@example.com",
            IsEnable = true,
            LoginFailCount = 0,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "admin"
        };
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);
        var ok = await sut.UpdateAsync(new UserUpdateRequest
        {
            Id = user.Id,
            UserName = "Charlie Updated",
            DeptId = "FIN",
            MobilePhone = "0900",
            Email = "c@example.com",
            IsEnable = false
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_UserInfos.FirstAsync(x => x.Id == user.Id);
        Assert.AreEqual("Charlie Updated", updated.UserName);
        Assert.AreEqual("FIN", updated.DeptId);
        Assert.AreEqual("0900", updated.MobilePhone);
        Assert.AreEqual("c@example.com", updated.Email);
        Assert.IsFalse(updated.IsEnable);
        Assert.IsNotNull(updated.UpdatedTime);
        Assert.AreEqual("system", updated.UpdatedId);
    }

    [TestMethod]
    public async Task DeleteAsync_ExistingUser_Should_ReturnTrue_AndRemove()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var user = new Sys_UserInfo
        {
            UserId = "delete-me",
            UserName = "Delete Me",
            Password = "HASH::D",
            DeptId = "IT",
            MobilePhone = "0911111111",
            Email = "delete@example.com",
            IsEnable = true,
            LoginFailCount = 0,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "admin"
        };
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);
        var ok = await sut.DeleteAsync(user.Id);

        Assert.IsTrue(ok);
        Assert.IsFalse(await db.Sys_UserInfos.AnyAsync(x => x.Id == user.Id));
    }

    [TestMethod]
    public async Task ResetPasswordAsync_ExistingUser_Should_HashAndResetFailCount()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var user = new Sys_UserInfo
        {
            UserId = "reset-me",
            UserName = "Reset Me",
            Password = "HASH::OLD",
            DeptId = "IT",
            MobilePhone = "0911111111",
            Email = "reset@example.com",
            IsEnable = true,
            LoginFailCount = 5,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "admin"
        };
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);
        var ok = await sut.ResetPasswordAsync(new UserResetPasswordRequest
        {
            Id = user.Id,
            NewPassword = "NewPass123"
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_UserInfos.FirstAsync(x => x.Id == user.Id);
        Assert.AreEqual("HASH::NewPass123", updated.Password);
        Assert.AreEqual(0, updated.LoginFailCount);
        Assert.IsTrue(updated.IsEnable); // 管理員重設密碼同時恢復啟用
        Assert.IsNotNull(updated.UpdatedTime);
        Assert.AreEqual("system", updated.UpdatedId);
    }

    [TestMethod]
    public async Task InvalidId_Should_Throw_ArgumentException()
    {
        using var scope = BuildScope();
        var sut = new UserService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => sut.GetByIdAsync(0));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => sut.DeleteAsync(0));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => sut.UpdateAsync(new UserUpdateRequest { Id = 0 }));
    }

    [TestMethod]
    public async Task ChangePasswordAsync_CorrectOldPassword_ShouldUpdateHash()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var user = new Sys_UserInfo
        {
            UserId = "change-pw",
            UserName = "Change Password",
            Password = "HASH::OldPass1",
            DeptId = "IT",
            MobilePhone = "0911111111",
            Email = "change@example.com",
            IsEnable = true,
            LoginFailCount = 0,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "admin"
        };
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);
        var ok = await sut.ChangePasswordAsync(new UserChangePasswordRequest
        {
            Id = user.Id,
            OldPassword = "OldPass1",
            NewPassword = "NewPass2024"
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_UserInfos.FirstAsync(x => x.Id == user.Id);
        Assert.AreEqual("HASH::NewPass2024", updated.Password);
        Assert.IsNotNull(updated.UpdatedTime);
        Assert.AreEqual("system", updated.UpdatedId);
    }

    [TestMethod]
    public async Task ChangePasswordAsync_WrongOldPassword_ShouldThrow_Unauthorized()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var user = new Sys_UserInfo
        {
            UserId = "change-pw-fail",
            UserName = "Change Password Fail",
            Password = "HASH::OldPass1",
            DeptId = "IT",
            MobilePhone = "0911111111",
            Email = "changefail@example.com",
            IsEnable = true,
            LoginFailCount = 0,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "admin"
        };
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
            sut.ChangePasswordAsync(new UserChangePasswordRequest
            {
                Id = user.Id,
                OldPassword = "WrongPass1",
                NewPassword = "NewPass2024"
            }));
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"user-service-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICryptographyService, FakeCryptographyService>();
        services.AddScoped<IPasswordManager, PasswordManager>();
        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private sealed class FakeCryptographyService : ICryptographyService
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

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = new CurrentUser { UserId = "system", Email = "system@localhost" };
    }
}

