using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.CryptographyService.Services;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.PasswordManager.Services;
using Template.BusinessRule.UserService.Services;
using Template.Common.Enums;
using Template.Common.Models;
using Template.Common.Models.User;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class UserServiceTests
{
    [TestMethod]
    public async Task CreateAsync_Should_CreateUser_AndHashPassword()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var department = await SeedDepartmentAsync(db, "IT");
        var sut = new UserService(scope.ServiceProvider);

        var created = await sut.CreateAsync(new UserCreateRequest
        {
            UserId = "  alice  ",
            UserName = "Alice User",
            Password = "ValidPass@123",
            DeptId = department.DeptId,
            MobilePhone = "0911222333",
            Email = "alice@example.com",
            IsEnable = true
        });

        Assert.IsTrue(created.Id > 0);
        Assert.AreEqual("alice", created.UserId);
        Assert.AreEqual("Alice User", created.UserName);
        Assert.AreEqual(department.DeptId, created.DeptId);
        Assert.AreEqual("IT", created.DeptName);

        var entity = await db.Sys_UserInfos.FirstAsync(x => x.Id == created.Id);
        Assert.AreEqual("HASH::ValidPass@123", entity.Password);

        var history = await db.Sys_UserPasswordHistories.SingleAsync(x => x.UserId == "alice");
        Assert.AreEqual("HASH::ValidPass@123", history.PasswordHash);
        Assert.AreEqual((int)UserPasswordChangeTypeEnum.Create, history.ChangeType);
        Assert.AreEqual("system", history.ChangedId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        var audit = AssertSingleAudit(logService, AuditActionEnum.Create, "alice");
        var newValueText = audit.NewValue?.ToString() ?? string.Empty;
        Assert.IsFalse(newValueText.Contains("ValidPass@123", StringComparison.Ordinal));
        Assert.IsFalse(newValueText.Contains("HASH::ValidPass@123", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task CreateAsync_DuplicateUserId_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var department = await SeedDepartmentAsync(db, "IT");
        db.Sys_UserInfos.Add(CreateUser("bob", department.DeptId));
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.CreateAsync(new UserCreateRequest
            {
                UserId = "bob",
                UserName = "Bob",
                DeptId = department.DeptId,
                Password = "AnotherPass@123"
            }));
    }

    [TestMethod]
    public async Task CreateAsync_DepartmentDoesNotExist_Should_Throw()
    {
        using var scope = BuildScope();
        var sut = new UserService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.CreateAsync(new UserCreateRequest
            {
                UserId = "alice",
                UserName = "Alice",
                DeptId = 999,
                Password = "ValidPass@123"
            }));
    }

    [TestMethod]
    public async Task GetListAsync_Should_Filter_ByKeyword_Status_AndDepartment()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var hr = await SeedDepartmentAsync(db, "HR");
        var it = await SeedDepartmentAsync(db, "IT");
        var apps = await SeedDepartmentAsync(db, "Applications", it.DeptId);

        db.Sys_UserInfos.AddRange(
            CreateUser("userA", hr.DeptId, "User A", "a@example.com", true),
            CreateUser("userB", it.DeptId, "User B", "b@example.com", false),
            CreateUser("userC", apps.DeptId, "User C", "c@example.com", true));

        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);

        var byKeyword = await sut.GetListAsync("example.com", null);
        var byStatus = await sut.GetListAsync(null, true);
        var byDepartment = await sut.GetListAsync(null, null, it.DeptId, includeSubDepartments: true);

        Assert.AreEqual(3, byKeyword.TotalCount);
        Assert.AreEqual(2, byStatus.TotalCount);
        Assert.AreEqual("userA", byStatus.Items[0].UserId);
        Assert.AreEqual(2, byDepartment.TotalCount);
        Assert.AreEqual("IT", byDepartment.Items[0].DeptName);
        Assert.AreEqual("Applications", byDepartment.Items[1].DeptName);
    }

    [TestMethod]
    public async Task UpdateAsync_ExistingUser_Should_ReturnTrue_AndPersist()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var hr = await SeedDepartmentAsync(db, "HR");
        var finance = await SeedDepartmentAsync(db, "Finance");
        var user = CreateUser("charlie", hr.DeptId, "Charlie User", "charlie@example.com");
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);
        var ok = await sut.UpdateAsync(new UserUpdateRequest
        {
            Id = user.Id,
            UserName = "Charlie Updated",
            DeptId = finance.DeptId,
            MobilePhone = "0900",
            Email = "c@example.com",
            IsEnable = false
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_UserInfos.FirstAsync(x => x.Id == user.Id);
        Assert.AreEqual("Charlie Updated", updated.UserName);
        Assert.AreEqual(finance.DeptId, updated.DeptId);
        Assert.AreEqual("0900", updated.MobilePhone);
        Assert.AreEqual("c@example.com", updated.Email);
        Assert.IsFalse(updated.IsEnable);
        Assert.IsNotNull(updated.UpdatedTime);
        Assert.AreEqual("system", updated.UpdatedId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        AssertSingleAudit(logService, AuditActionEnum.Update, user.UserId);
    }

    [TestMethod]
    public async Task UpdateAsync_DepartmentDoesNotExist_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var department = await SeedDepartmentAsync(db, "IT");
        var user = CreateUser("alice", department.DeptId);
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.UpdateAsync(new UserUpdateRequest
            {
                Id = user.Id,
                UserName = "Alice",
                DeptId = 999
            }));
    }

    [TestMethod]
    public async Task DeleteAsync_ExistingUser_Should_ReturnTrue_AndRemove()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var department = await SeedDepartmentAsync(db, "IT");
        var user = CreateUser("delete-me", department.DeptId, "Delete Me", "delete@example.com");
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);
        var ok = await sut.DeleteAsync(user.Id);

        Assert.IsTrue(ok);
        Assert.IsFalse(await db.Sys_UserInfos.AnyAsync(x => x.Id == user.Id));

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        AssertSingleAudit(logService, AuditActionEnum.Delete, user.UserId);
    }

    [TestMethod]
    public async Task DeleteAsync_UserHasPasswordHistory_Should_RemoveUser_AndKeepHistory()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var department = await SeedDepartmentAsync(db, "IT");
        var sut = new UserService(scope.ServiceProvider);

        var created = await sut.CreateAsync(new UserCreateRequest
        {
            UserId = "history-user",
            UserName = "History User",
            Password = "ValidPass@123",
            DeptId = department.DeptId,
            MobilePhone = "0911222333",
            Email = "history@example.com",
            IsEnable = true
        });

        var ok = await sut.DeleteAsync(created.Id);

        Assert.IsTrue(ok);
        Assert.IsFalse(await db.Sys_UserInfos.AnyAsync(x => x.Id == created.Id));
        Assert.IsTrue(await db.Sys_UserPasswordHistories.AnyAsync(x => x.UserId == "history-user"));
    }

    [TestMethod]
    public async Task ResetPasswordAsync_ExistingUser_Should_HashAndResetFailCount()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var department = await SeedDepartmentAsync(db, "IT");
        var user = CreateUser("reset-me", department.DeptId, "Reset Me", "reset@example.com");
        user.Password = "HASH::OLD";
        user.LoginFailCount = 5;
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);
        var ok = await sut.ResetPasswordAsync(new UserResetPasswordRequest
        {
            Id = user.Id,
            NewPassword = "NewPass@1234"
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_UserInfos.FirstAsync(x => x.Id == user.Id);
        Assert.AreEqual("HASH::NewPass@1234", updated.Password);
        Assert.AreEqual(0, updated.LoginFailCount);
        Assert.IsTrue(updated.IsEnable);
        Assert.IsNotNull(updated.UpdatedTime);
        Assert.AreEqual("system", updated.UpdatedId);

        var history = await db.Sys_UserPasswordHistories.SingleAsync(x => x.UserId == user.UserId);
        Assert.AreEqual("HASH::NewPass@1234", history.PasswordHash);
        Assert.AreEqual((int)UserPasswordChangeTypeEnum.Reset, history.ChangeType);
        Assert.AreEqual("system", history.ChangedId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        var audit = AssertSingleAudit(logService, AuditActionEnum.PasswordReset, user.UserId);
        Assert.IsFalse((audit.Metadata?.ToString() ?? string.Empty).Contains("NewPass@1234", StringComparison.Ordinal));
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
        var department = await SeedDepartmentAsync(db, "IT");
        var user = CreateUser("change-pw", department.DeptId, "Change Password", "change@example.com");
        user.Password = "HASH::OldPass@123";
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);
        var ok = await sut.ChangePasswordAsync(new UserChangePasswordRequest
        {
            Id = user.Id,
            OldPassword = "OldPass@123",
            NewPassword = "NewPass@2024"
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_UserInfos.FirstAsync(x => x.Id == user.Id);
        Assert.AreEqual("HASH::NewPass@2024", updated.Password);
        Assert.IsNotNull(updated.UpdatedTime);
        Assert.AreEqual("system", updated.UpdatedId);

        var history = await db.Sys_UserPasswordHistories.SingleAsync(x => x.UserId == user.UserId);
        Assert.AreEqual("HASH::NewPass@2024", history.PasswordHash);
        Assert.AreEqual((int)UserPasswordChangeTypeEnum.Change, history.ChangeType);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        var audit = AssertSingleAudit(logService, AuditActionEnum.PasswordChange, user.UserId);
        Assert.IsFalse((audit.Metadata?.ToString() ?? string.Empty).Contains("NewPass@2024", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task ChangePasswordAsync_WrongOldPassword_ShouldThrow_Unauthorized()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var department = await SeedDepartmentAsync(db, "IT");
        var user = CreateUser("change-pw-fail", department.DeptId, "Change Password Fail", "changefail@example.com");
        user.Password = "HASH::OldPass@123";
        db.Sys_UserInfos.Add(user);
        await db.SaveChangesAsync();

        var sut = new UserService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() =>
            sut.ChangePasswordAsync(new UserChangePasswordRequest
            {
                Id = user.Id,
                OldPassword = "WrongPass@123",
                NewPassword = "NewPass@2024"
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
        services.AddSingleton<RecordingLogService>();
        services.AddScoped<ILogService>(sp => sp.GetRequiredService<RecordingLogService>());

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private static async Task<Sys_Department> SeedDepartmentAsync(ProjectDbContext db, string deptName, int? parentDeptId = null)
    {
        var department = new Sys_Department
        {
            DeptName = deptName,
            ParentDeptId = parentDeptId,
            SortOrder = 10,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };

        db.Sys_Departments.Add(department);
        await db.SaveChangesAsync();
        return department;
    }

    private static Sys_UserInfo CreateUser(
        string userId,
        int deptId,
        string? userName = null,
        string? email = null,
        bool isEnable = true)
    {
        return new Sys_UserInfo
        {
            UserId = userId,
            UserName = userName ?? userId,
            Password = "HASH::password",
            DeptId = deptId,
            MobilePhone = "0911111111",
            Email = email ?? $"{userId}@example.com",
            IsEnable = isEnable,
            LoginFailCount = 0,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "admin",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "admin"
        };
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

    private static UserOperationLogCreateRequest AssertSingleAudit(
        RecordingLogService logService,
        AuditActionEnum action,
        string targetUserId)
    {
        var audits = logService.Audits
            .Where(x => x.Action == action && x.TargetId == targetUserId)
            .ToList();

        Assert.AreEqual(1, audits.Count);
        return audits[0];
    }

    private sealed class RecordingLogService : ILogService
    {
        public List<UserOperationLogCreateRequest> Audits { get; } = [];

        public Task<long> WriteUserOperationAsync(
            UserOperationLogCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            Audits.Add(request);
            return Task.FromResult((long)Audits.Count);
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
