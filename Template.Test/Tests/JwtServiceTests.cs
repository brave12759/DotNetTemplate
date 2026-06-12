using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.TokenRevocationService.Services;
using Template.Common.Models.Jwt;
using Template.DataAccess.ProjectDbContext;
using Template.WebApi.Services;

namespace Template.Test.Tests;

[TestClass]
public class JwtServiceTests
{
    [TestMethod]
    public async Task GetSettingsAsync_Should_ReadJwtSettingsFromDatabase()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        SeedJwtExpirationSettings(db);
        await db.SaveChangesAsync();

        var sut = BuildJwtService(scope);
        var settings = await sut.GetSettingsAsync();

        Assert.AreEqual(ValidSecretKey, settings.SecretKey);
        Assert.AreEqual("template-api", settings.Issuer);
        Assert.AreEqual("template-client", settings.Audience);
        Assert.AreEqual(15, settings.PersonalTokenExpire);
        Assert.AreEqual(3, settings.ServerTokenExpire);
    }

    [TestMethod]
    public async Task GeneratePersonalTokenAsync_AndValidateTokenAsync_Should_ReturnPersonalClaims()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        SeedJwtExpirationSettings(db);
        await db.SaveChangesAsync();

        var sut = BuildJwtService(scope);
        var token = await sut.GeneratePersonalTokenAsync(
            "alice",
            "alice@example.com",
            "0911111111",
            "10",
            "127.0.0.1",
            "[\"Admin\"]",
            "[\"System.User:Manage\"]");

        var principal = await sut.ValidateTokenAsync(token);

        Assert.IsNotNull(principal);
        Assert.AreEqual("alice", principal.FindFirst(ClaimTypes.Name)?.Value);
        Assert.AreEqual("10", principal.FindFirst("dept_id")?.Value);
        Assert.AreEqual("personal", principal.FindFirst("token_type")?.Value);
        Assert.AreEqual("[\"Admin\"]", principal.FindFirst("role_groups")?.Value);
        Assert.AreEqual("[\"System.User:Manage\"]", principal.FindFirst("function_permissions")?.Value);
    }

    [TestMethod]
    public async Task GenerateServerTokenAsync_AndValidateTokenAsync_Should_ReturnServerClaims()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        SeedJwtExpirationSettings(db);
        await db.SaveChangesAsync();

        var sut = BuildJwtService(scope);
        var token = await sut.GenerateServerTokenAsync("erp-system", "10.0.0.1");
        var principal = await sut.ValidateTokenAsync(token);

        Assert.IsNotNull(principal);
        Assert.AreEqual("erp-system", principal.FindFirst("client_id")?.Value);
        Assert.AreEqual("server", principal.FindFirst("token_type")?.Value);
        Assert.AreEqual("10.0.0.1", principal.FindFirst("ip")?.Value);
    }

    [TestMethod]
    public async Task ValidateTokenAsync_RevokedToken_Should_ReturnNull()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var revocation = scope.ServiceProvider.GetRequiredService<RecordingTokenRevocationService>();
        SeedJwtExpirationSettings(db);
        await db.SaveChangesAsync();

        var sut = BuildJwtService(scope);
        var token = await sut.GeneratePersonalTokenAsync("alice", "alice@example.com", "0911111111", "10", "127.0.0.1");
        var principalBeforeRevoked = await sut.ValidateTokenAsync(token);
        var tokenId = principalBeforeRevoked?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        Assert.IsFalse(string.IsNullOrWhiteSpace(tokenId));
        revocation.Revoke(tokenId!, DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds());

        var principal = await sut.ValidateTokenAsync(token);

        Assert.IsNull(principal);
    }

    [TestMethod]
    public async Task UpdateSettingsAsync_Should_UpsertAndValidateSettings()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        SeedJwtExpirationSettings(db);
        await db.SaveChangesAsync();

        var sut = BuildJwtService(scope);
        await sut.UpdateSettingsAsync(new JwtSettingUpdateRequest
        {
            PersonalTokenExpire = 30,
            ServerTokenExpire = 5
        }, "admin");

        var settings = await sut.GetSettingsAsync();

        Assert.AreEqual(ValidSecretKey, settings.SecretKey);
        Assert.AreEqual("template-api", settings.Issuer);
        Assert.AreEqual("template-client", settings.Audience);
        Assert.AreEqual(30, settings.PersonalTokenExpire);
        Assert.AreEqual(5, settings.ServerTokenExpire);
    }

    [TestMethod]
    public async Task GetSettingsAsync_MissingExpirationSetting_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        AddJwtSetting(db, "PersonalTokenExpire", "15");
        await db.SaveChangesAsync();

        var sut = BuildJwtService(scope);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => sut.GetSettingsAsync());
    }

    [TestMethod]
    public async Task GetSettingsAsync_MissingJwtCoreConfiguration_Should_Throw()
    {
        using var scope = BuildScope(includeJwtCoreSettings: false);
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        SeedJwtExpirationSettings(db);
        await db.SaveChangesAsync();

        var sut = BuildJwtService(scope);

        var ex = await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => sut.GetSettingsAsync());
        StringAssert.Contains(ex.Message, "JwtSettings__SecretKey");
    }

    private const string ValidSecretKey = "12345678901234567890123456789012";

    private static IServiceScope BuildScope(bool includeJwtCoreSettings = true)
    {
        var services = new ServiceCollection();

        var configData = includeJwtCoreSettings
            ? new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = ValidSecretKey,
                ["JwtSettings:Issuer"] = "template-api",
                ["JwtSettings:Audience"] = "template-client"
            }
            : new Dictionary<string, string?>();

        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build());

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"jwt-service-tests-{Guid.NewGuid()}"));
        services.AddSingleton<RecordingTokenRevocationService>();
        services.AddSingleton<ITokenRevocationService>(sp => sp.GetRequiredService<RecordingTokenRevocationService>());

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private static JwtService BuildJwtService(IServiceScope scope) =>
        new(
            scope.ServiceProvider.GetRequiredService<ProjectDbContext>(),
            scope.ServiceProvider.GetRequiredService<ITokenRevocationService>(),
            scope.ServiceProvider.GetRequiredService<IConfiguration>());

    private static void SeedJwtExpirationSettings(ProjectDbContext db)
    {
        AddJwtSetting(db, "PersonalTokenExpire", "15");
        AddJwtSetting(db, "ServerTokenExpire", "3");
    }

    private static void AddJwtSetting(ProjectDbContext db, string key, string value)
    {
        db.Sys_BasicSettings.Add(new Sys_BasicSetting
        {
            Type = "JwtSetting",
            Key = key,
            Value = value,
            Label = key,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });
    }

    private sealed class RecordingTokenRevocationService : ITokenRevocationService
    {
        private readonly HashSet<string> _revokedTokenIds = [];

        public void Revoke(string tokenId, long expiredUnixTimeSeconds)
        {
            _revokedTokenIds.Add(tokenId);
        }

        public bool IsRevoked(string tokenId) => _revokedTokenIds.Contains(tokenId);
    }
}
