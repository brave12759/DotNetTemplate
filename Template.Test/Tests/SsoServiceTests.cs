using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.CryptographyService.Services;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.PasswordManager.Services;
using Template.Common.Enums;
using Template.BusinessRule.SsoService.Enums;
using Template.BusinessRule.SsoService.Exceptions;
using Template.BusinessRule.SsoService.Models;
using Template.BusinessRule.SsoService.Services;
using Template.Common.Models;
using Template.Common.Models.Jwt;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class SsoServiceTests
{
    [TestMethod]
    public async Task GetClientsAsync_WithFilterAndPaging_Should_ReturnFilteredItems()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.AddRange(
            CreateClient("erp-system", "ERP 系統", "Secret@123456"),
            CreateClient("m3u8-client", "M3U8 系統", "Secret@123456", isEnable: false));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var result = await sut.GetClientsAsync("erp", true, enablePaging: true, page: 1, pageSize: 10);

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual("erp-system", result.Items.Single().ClientId);
    }

    [TestMethod]
    public async Task CreateClientAsync_Should_HashSecret_AndSetAuditFields()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var sut = new SsoService(scope.ServiceProvider);

        var created = await sut.CreateClientAsync(new SsoClientCreateRequest
        {
            ClientId = "  erp-system  ",
            ClientName = "  ERP 系統  ",
            ClientSecret = "Secret@123456",
            IsEnable = true
        });

        Assert.IsTrue(created.Id > 0);
        Assert.AreEqual("erp-system", created.ClientId);
        Assert.AreEqual("ERP 系統", created.ClientName);
        Assert.AreEqual("tester", created.CreatedId);
        Assert.AreEqual("tester", created.UpdatedId);

        var entity = await db.Sso_Clients.FirstAsync(x => x.Id == created.Id);
        Assert.AreEqual("HASH::Secret@123456", entity.ClientSecretHash);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.UserOperations.Count);
        Assert.AreEqual("SSO", logService.UserOperations[0].Module);
        Assert.AreEqual(AuditActionEnum.Create, logService.UserOperations[0].Action);
    }

    [TestMethod]
    public async Task CreateClientAsync_DuplicateClientId_Should_ThrowMessageCode()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.Add(CreateClient("erp-system", "ERP 系統", "Secret@123456"));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);

        var ex = await Assert.ThrowsExceptionAsync<SsoMessageException>(() =>
            sut.CreateClientAsync(new SsoClientCreateRequest
            {
                ClientId = "erp-system",
                ClientName = "ERP 系統 2",
                ClientSecret = "Another@123456",
                IsEnable = true
            }));

        Assert.AreEqual(SsoMessageEnum.ClientIdAlreadyExists, ex.MessageCode);
    }

    [TestMethod]
    public async Task LoginAsync_ValidClient_Should_ReturnServerToken()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.Add(CreateClient("erp-system", "ERP 系統", "Secret@123456"));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var result = await sut.LoginAsync("erp-system", "Secret@123456", "127.0.0.1");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("server-token-for-erp-system-from-127.0.0.1", result.Token);
        Assert.IsNull(result.MessageCode);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.SsoLogs.Count);
        Assert.AreEqual("Login", logService.SsoLogs[0].EventName);
        Assert.AreEqual("Success", logService.SsoLogs[0].Result);
        var loginMetaJson = System.Text.Json.JsonSerializer.Serialize(logService.SsoLogs[0].Metadata);
        StringAssert.Contains(loginMetaJson, "\"Reason\":\"Success\"");
    }

    [TestMethod]
    public async Task UpdateClientAsync_WithNewSecret_Should_RotateSecretHash()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var client = CreateClient("erp-system", "ERP 系統", "OldSecret@123456");
        db.Sso_Clients.Add(client);
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var updated = await sut.UpdateClientAsync(new SsoClientUpdateRequest
        {
            Id = client.Id,
            ClientName = "ERP 系統新版",
            ClientSecret = "NewSecret@123456",
            IsEnable = true
        });

        Assert.IsTrue(updated);

        var entity = await db.Sso_Clients.FirstAsync(x => x.Id == client.Id);
        Assert.AreEqual("ERP 系統新版", entity.ClientName);
        Assert.AreEqual("HASH::NewSecret@123456", entity.ClientSecretHash);
        Assert.AreEqual("tester", entity.UpdatedId);
    }

    [TestMethod]
    public async Task LoginAsync_InvalidSecret_Should_ReturnInvalidClientCredentialsCode()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.Add(CreateClient("erp-system", "ERP 系統", "Secret@123456"));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var result = await sut.LoginAsync("erp-system", "WrongSecret", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SsoMessageEnum.InvalidClientCredentials, result.MessageCode);
        Assert.AreEqual("ClientId 或 ClientSecret 錯誤。", result.ErrorMessage);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.SsoLogs.Count);
        Assert.AreEqual("Failure", logService.SsoLogs[0].Result);
        var wrongSecretMetaJson = System.Text.Json.JsonSerializer.Serialize(logService.SsoLogs[0].Metadata);
        StringAssert.Contains(wrongSecretMetaJson, "\"Reason\":\"WrongSecret\"");
    }

    [TestMethod]
    public async Task LoginAsync_DisabledClient_Should_ReturnInvalidClientCredentialsCode()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.Add(CreateClient("disabled-client", "停用系統", "Secret@123456", isEnable: false));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var result = await sut.LoginAsync("disabled-client", "Secret@123456", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SsoMessageEnum.InvalidClientCredentials, result.MessageCode);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.SsoLogs.Count);
        Assert.AreEqual("Failure", logService.SsoLogs[0].Result);
        var disabledMetaJson = System.Text.Json.JsonSerializer.Serialize(logService.SsoLogs[0].Metadata);
        StringAssert.Contains(disabledMetaJson, "\"Reason\":\"ClientNotFoundOrDisabled\"");
    }

    [TestMethod]
    public async Task ValidateTokenAsync_ServerTokenForEnabledClient_Should_ReturnValid()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.Add(CreateClient("erp-system", "ERP 系統", "Secret@123456"));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var result = await sut.ValidateTokenAsync("valid-server-token");

        Assert.IsTrue(result.IsValid);
        Assert.AreEqual("erp-system", result.ClientId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeSeconds(1893456000), result.ExpiresAt);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.SsoLogs.Count);
        Assert.AreEqual("ValidateToken", logService.SsoLogs[0].EventName);
        Assert.AreEqual("Success", logService.SsoLogs[0].Result);
        var validateSuccessMetaJson = System.Text.Json.JsonSerializer.Serialize(logService.SsoLogs[0].Metadata);
        StringAssert.Contains(validateSuccessMetaJson, "\"Reason\":\"Success\"");
    }

    [TestMethod]
    public async Task ValidateTokenAsync_ServerTokenForDisabledClient_Should_ReturnInvalid()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.Add(CreateClient("erp-system", "ERP 系統", "Secret@123456", isEnable: false));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var result = await sut.ValidateTokenAsync("valid-server-token");

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("erp-system", result.ClientId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.SsoLogs.Count);
        Assert.AreEqual("Failure", logService.SsoLogs[0].Result);
        var disabledClientMetaJson = System.Text.Json.JsonSerializer.Serialize(logService.SsoLogs[0].Metadata);
        StringAssert.Contains(disabledClientMetaJson, "\"Reason\":\"ClientTypeOrStateMismatch\"");
    }

    [TestMethod]
    public async Task ValidateTokenAsync_PersonalToken_Should_ReturnInvalid()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.Add(CreateClient("erp-system", "ERP 系統", "Secret@123456"));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var result = await sut.ValidateTokenAsync("personal-token");

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual("erp-system", result.ClientId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.SsoLogs.Count);
        Assert.AreEqual("Failure", logService.SsoLogs[0].Result);
        var personalTokenMetaJson = System.Text.Json.JsonSerializer.Serialize(logService.SsoLogs[0].Metadata);
        StringAssert.Contains(personalTokenMetaJson, "\"Reason\":\"ClientTypeOrStateMismatch\"");
    }

    [TestMethod]
    public async Task RefreshAsync_ExpiredServerTokenForEnabledClient_Should_ReturnNewToken()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.Add(CreateClient("erp-system", "ERP 系統", "Secret@123456"));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var result = await sut.RefreshAsync("expired-server-token", "127.0.0.1");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("server-token-for-erp-system-from-127.0.0.1", result.Token);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.SsoLogs.Count);
        Assert.AreEqual("RefreshToken", logService.SsoLogs[0].EventName);
        Assert.AreEqual("Success", logService.SsoLogs[0].Result);
    }

    [TestMethod]
    public async Task RefreshAsync_PersonalToken_Should_ReturnFail()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sso_Clients.Add(CreateClient("erp-system", "ERP 系統", "Secret@123456"));
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var result = await sut.RefreshAsync("expired-personal-token", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SsoMessageEnum.InvalidClientCredentials, result.MessageCode);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(logService.SsoLogs.Single().Metadata);
        StringAssert.Contains(metadataJson, "\"Reason\":\"ClientTypeOrStateMismatch\"");
    }

    [TestMethod]
    public async Task DeleteClientAsync_ExistingClient_Should_DeleteAndWriteAuditLog()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var client = CreateClient("erp-system", "ERP 系統", "Secret@123456");
        db.Sso_Clients.Add(client);
        await db.SaveChangesAsync();

        var sut = new SsoService(scope.ServiceProvider);
        var deleted = await sut.DeleteClientAsync(client.Id);

        Assert.IsTrue(deleted);
        Assert.IsFalse(await db.Sso_Clients.AnyAsync(x => x.Id == client.Id));

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.IsTrue(logService.UserOperations.Any(x => x.Action == AuditActionEnum.Delete));
    }

    [TestMethod]
    public async Task DeleteClientAsync_NotFound_Should_ReturnFalse()
    {
        using var scope = BuildScope();
        var sut = new SsoService(scope.ServiceProvider);

        var deleted = await sut.DeleteClientAsync(999);

        Assert.IsFalse(deleted);
    }

    [TestMethod]
    public async Task LoginAsync_MissingCredential_Should_ReturnFailAndWriteReason()
    {
        using var scope = BuildScope();
        var sut = new SsoService(scope.ServiceProvider);

        var result = await sut.LoginAsync("", "", "127.0.0.1");

        Assert.IsFalse(result.Success);
        Assert.AreEqual(SsoMessageEnum.InvalidClientCredentials, result.MessageCode);
        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(logService.SsoLogs.Single().Metadata);
        StringAssert.Contains(metadataJson, "\"Reason\":\"MissingCredential\"");
    }

    [TestMethod]
    public async Task ValidateTokenAsync_InvalidToken_Should_ReturnInvalidAndWriteReason()
    {
        using var scope = BuildScope();
        var sut = new SsoService(scope.ServiceProvider);

        var result = await sut.ValidateTokenAsync("invalid-token");

        Assert.IsFalse(result.IsValid);
        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(logService.SsoLogs.Single().Metadata);
        StringAssert.Contains(metadataJson, "\"Reason\":\"InvalidToken\"");
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"sso-service-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();
        services.AddScoped<ICryptographyService, FakeHashOnlyCryptographyService>();
        services.AddScoped<IPasswordManager, PasswordManager>();
        services.AddScoped<IJwtService, FakeJwtService>();
        services.AddSingleton<RecordingLogService>();
        services.AddScoped<ILogService>(sp => sp.GetRequiredService<RecordingLogService>());

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private static Sso_Client CreateClient(
        string clientId,
        string clientName,
        string secret,
        bool isEnable = true)
    {
        return new Sso_Client
        {
            ClientId = clientId,
            ClientName = clientName,
            ClientSecretHash = $"HASH::{secret}",
            IsEnable = isEnable,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = new() { UserId = "tester", Email = "tester@localhost" };
    }

    private sealed class RecordingLogService : ILogService
    {
        public List<UserOperationLogCreateRequest> UserOperations { get; } = [];
        public List<SsoLogCreateRequest> SsoLogs { get; } = [];

        public Task<long> WriteUserOperationAsync(UserOperationLogCreateRequest request, CancellationToken cancellationToken = default)
        {
            UserOperations.Add(request);
            return Task.FromResult((long)UserOperations.Count);
        }

        public Task<UserOperationLogQueryResult> GetUserOperationLogsAsync(UserOperationLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<long> WriteQueueAsync(QueueLogCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<QueueLogQueryResult> GetQueueLogsAsync(QueueLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<long> WriteSsoAsync(SsoLogCreateRequest request, CancellationToken cancellationToken = default)
        {
            SsoLogs.Add(request);
            return Task.FromResult((long)SsoLogs.Count);
        }

        public Task<SsoLogQueryResult> GetSsoLogsAsync(SsoLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

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
            Task.FromResult($"personal-token-for-{userId}");

        public Task<string> GenerateServerTokenAsync(string clientId, string ip) =>
            Task.FromResult($"server-token-for-{clientId}-from-{ip}");

        public Task<ClaimsPrincipal?> ValidateTokenAsync(string token, bool validateRevocation = true)
        {
            return token switch
            {
                "valid-server-token" => Task.FromResult<ClaimsPrincipal?>(CreatePrincipal("server")),
                "personal-token" => Task.FromResult<ClaimsPrincipal?>(CreatePrincipal("personal")),
                _ => Task.FromResult<ClaimsPrincipal?>(null)
            };
        }

        public Task<ClaimsPrincipal?> ValidateExpiredTokenAsync(string token)
        {
            return token switch
            {
                "expired-server-token" => Task.FromResult<ClaimsPrincipal?>(CreatePrincipal("server")),
                "expired-personal-token" => Task.FromResult<ClaimsPrincipal?>(CreatePrincipal("personal")),
                _ => Task.FromResult<ClaimsPrincipal?>(null)
            };
        }

        public Task<JwtSettingDto> GetSettingsAsync() => throw new NotImplementedException();

        public Task UpdateSettingsAsync(JwtSettingUpdateRequest request, string updatedBy) => throw new NotImplementedException();

        private static ClaimsPrincipal CreatePrincipal(string tokenType)
        {
            var identity = new ClaimsIdentity(
                [
                    new Claim("token_type", tokenType),
                    new Claim("client_id", "erp-system"),
                    new Claim(JwtRegisteredClaimNames.Exp, "1893456000")
                ],
                "TestAuth");

            return new ClaimsPrincipal(identity);
        }
    }
}
