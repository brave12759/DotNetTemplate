using System.Reflection;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.Settings;
using Template.WebApi.Authentication;
using Template.WebApi.Filters;

namespace Template.Test.Tests;

[TestClass]
public class DevBypassAuthenticationHandlerTests
{
    [TestMethod]
    public async Task Authenticate_NoAuthorizationHeader_Should_ReturnDevBypassPrincipal()
    {
        var handler = CreateHandler(new TestAuthenticationService());
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(new TestAuthenticationService())
            .BuildServiceProvider();

        await handler.InitializeAsync(CreateScheme(), context);
        var result = await handler.AuthenticateAsync();

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.Principal);
        Assert.AreEqual("dev-user", result.Principal.FindFirstValue(ClaimTypes.Name));
        Assert.AreEqual("dev@test.local", result.Principal.FindFirstValue(ClaimTypes.Email));
        Assert.AreEqual("D01", result.Principal.FindFirstValue("dept_id"));
        Assert.AreEqual("127.0.0.1", result.Principal.FindFirstValue("ip"));
    }

    [TestMethod]
    public async Task Authenticate_WithAuthorizationHeader_Should_DelegateToJwtBearerScheme()
    {
        var authService = new TestAuthenticationService
        {
            AuthenticateResult = AuthenticateResult.Fail("invalid token")
        };

        var handler = CreateHandler(authService);
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer mocked";
        context.RequestServices = new ServiceCollection()
            .AddSingleton<IAuthenticationService>(authService)
            .BuildServiceProvider();

        await handler.InitializeAsync(CreateScheme(), context);
        var result = await handler.AuthenticateAsync();

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(JwtBearerDefaults.AuthenticationScheme, authService.LastAuthenticateScheme);
        Assert.IsNotNull(result.Failure);
        StringAssert.Contains(result.Failure.Message, "invalid token");
    }

    private static DevBypassAuthenticationHandler CreateHandler(IAuthenticationService authService)
    {
        var options = new TestOptionsMonitor<DevBypassAuthenticationOptions>(new DevBypassAuthenticationOptions());
        var devUser = new DevBypassUserSettings
        {
            UserId = "dev-user",
            Email = "dev@test.local",
            MobilePhone = "0912345678",
            DeptId = "D01"
        };

        return new DevBypassAuthenticationHandler(options, NullLoggerFactory.Instance, UrlEncoder.Default, devUser);
    }

    private static AuthenticationScheme CreateScheme()
        => new(DevBypassAuthenticationHandler.SchemeName, null, typeof(DevBypassAuthenticationHandler));

    private sealed class TestAuthenticationService : IAuthenticationService
    {
        public AuthenticateResult AuthenticateResult { get; set; } = AuthenticateResult.NoResult();
        public string? LastAuthenticateScheme { get; private set; }

        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
        {
            LastAuthenticateScheme = scheme;
            return Task.FromResult(AuthenticateResult);
        }

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }

    private sealed class TestOptionsMonitor<T>(T value) : IOptionsMonitor<T> where T : class
    {
        public T CurrentValue => value;

        public T Get(string? name) => value;

        public IDisposable? OnChange(Action<T, string?> listener) => null;
    }
}

[TestClass]
public class ProgramUtilityTests
{
    [TestMethod]
    public void ValidateBackgroundQueueSettings_Valid_Should_NotThrow()
    {
        InvokeValidateBackgroundQueueSettings(new BackgroundQueueSettings
        {
            DefaultPollingIntervalSeconds = 5,
            DefaultLockTimeoutSeconds = 60,
            DefaultMaxRetryCount = 0,
            ShutdownTimeoutSeconds = 10
        });
    }

    [TestMethod]
    public void ValidateBackgroundQueueSettings_NonPositivePolling_Should_Throw()
    {
        var ex = Assert.ThrowsException<InvalidOperationException>(() => InvokeValidateBackgroundQueueSettings(new BackgroundQueueSettings
        {
            DefaultPollingIntervalSeconds = 0,
            DefaultLockTimeoutSeconds = 60,
            DefaultMaxRetryCount = 1,
            ShutdownTimeoutSeconds = 10
        }));

        StringAssert.Contains(ex.Message, "DefaultPollingIntervalSeconds");
    }

    [TestMethod]
    public void ValidateBackgroundQueueSettings_NegativeRetry_Should_Throw()
    {
        var ex = Assert.ThrowsException<InvalidOperationException>(() => InvokeValidateBackgroundQueueSettings(new BackgroundQueueSettings
        {
            DefaultPollingIntervalSeconds = 1,
            DefaultLockTimeoutSeconds = 60,
            DefaultMaxRetryCount = -1,
            ShutdownTimeoutSeconds = 10
        }));

        StringAssert.Contains(ex.Message, "DefaultMaxRetryCount");
    }

    [TestMethod]
    public void ReadDotEnv_Should_ParseQuotesExportAndComments()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dotenv-{Guid.NewGuid():N}.env");
        File.WriteAllText(path,
            "# comment\n" +
            "A=1\n" +
            "export B=2\n" +
            "C=\"x\\n y\"\n" +
            "D='single quoted'\n" +
            "E=\n");

        try
        {
            var values = InvokeReadDotEnv(path);

            Assert.AreEqual("1", values["A"]);
            Assert.AreEqual("2", values["B"]);
            Assert.AreEqual("x\n y", values["C"]);
            Assert.AreEqual("single quoted", values["D"]);
            Assert.AreEqual(string.Empty, values["E"]);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void LoadDotEnv_Should_MergeAndNotOverrideExistingEnvironmentVariable()
    {
        var root = Path.Combine(Path.GetTempPath(), $"dotenv-root-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var envName = "Unit";

        var keyA = $"UT_DOTENV_A_{Guid.NewGuid():N}";
        var keyB = $"UT_DOTENV_B_{Guid.NewGuid():N}";
        var keyC = $"UT_DOTENV_C_{Guid.NewGuid():N}";

        File.WriteAllText(Path.Combine(root, ".env"), $"{keyA}=from-dotenv\n{keyB}=from-dotenv\n");
        File.WriteAllText(Path.Combine(root, $".env.{envName}"), $"{keyB}=from-env-name\n{keyC}=from-env-name\n");

        Environment.SetEnvironmentVariable(keyA, null);
        Environment.SetEnvironmentVariable(keyB, "preexisting");
        Environment.SetEnvironmentVariable(keyC, null);

        try
        {
            InvokeLoadDotEnv(root, envName);

            Assert.AreEqual("from-dotenv", Environment.GetEnvironmentVariable(keyA));
            Assert.AreEqual("preexisting", Environment.GetEnvironmentVariable(keyB));
            Assert.AreEqual("from-env-name", Environment.GetEnvironmentVariable(keyC));
        }
        finally
        {
            Environment.SetEnvironmentVariable(keyA, null);
            Environment.SetEnvironmentVariable(keyB, null);
            Environment.SetEnvironmentVariable(keyC, null);
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void GetRequiredJwtCoreSettings_MissingSecret_Should_Throw()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "template-api",
                ["JwtSettings:Audience"] = "template-client"
            })
            .Build();

        var ex = Assert.ThrowsException<InvalidOperationException>(() => InvokeGetRequiredJwtCoreSettings(configuration));
        StringAssert.Contains(ex.Message, "JwtSettings:SecretKey");
    }

    [TestMethod]
    public void GetRequiredJwtCoreSettings_ValidSettings_Should_ReturnDto()
    {
        var secretKey = "12345678901234567890123456789012";
        var issuer = "template-api";
        var audience = "template-client";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = secretKey,
                ["JwtSettings:Issuer"] = issuer,
                ["JwtSettings:Audience"] = audience
            })
            .Build();

        var dto = InvokeGetRequiredJwtCoreSettings(configuration);
        Assert.AreEqual(secretKey, dto.SecretKey);
        Assert.AreEqual(issuer, dto.Issuer);
        Assert.AreEqual(audience, dto.Audience);
    }

    private static Type GetProgramType()
    {
        return typeof(ResponseWrapperFilter).Assembly.GetType("Program")
            ?? throw new AssertFailedException("Program type was not found in Template.WebApi assembly.");
    }

    private static void InvokeValidateBackgroundQueueSettings(BackgroundQueueSettings settings)
    {
        var programType = GetProgramType();
        var method = programType
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
                m.Name.Contains("ValidateBackgroundQueueSettings", StringComparison.Ordinal) &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(BackgroundQueueSettings))
            ?? throw new AssertFailedException("ValidateBackgroundQueueSettings method was not found.");

        try
        {
            method.Invoke(null, [settings]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }

    private static Dictionary<string, string> InvokeReadDotEnv(string path)
    {
        var programType = GetProgramType();
        var method = programType
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
                m.Name.Contains("ReadDotEnv", StringComparison.Ordinal) &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(string))
            ?? throw new AssertFailedException("ReadDotEnv method was not found.");

        var result = method.Invoke(null, [path]);
        return result as Dictionary<string, string>
            ?? throw new AssertFailedException("ReadDotEnv result type is unexpected.");
    }

    private static void InvokeLoadDotEnv(string contentRootPath, string environmentName)
    {
        var programType = GetProgramType();
        var method = programType
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
                m.Name.Contains("LoadDotEnv", StringComparison.Ordinal) &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == typeof(string) &&
                m.GetParameters()[1].ParameterType == typeof(string))
            ?? throw new AssertFailedException("LoadDotEnv method was not found.");

        method.Invoke(null, [contentRootPath, environmentName]);
    }

    private static Template.Common.Models.Jwt.JwtSettingDto InvokeGetRequiredJwtCoreSettings(IConfiguration configuration)
    {
        var programType = GetProgramType();
        var method = programType
            .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
                m.Name.Contains("GetRequiredJwtCoreSettings", StringComparison.Ordinal) &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == typeof(IConfiguration))
            ?? throw new AssertFailedException("GetRequiredJwtCoreSettings method was not found.");

        try
        {
            var result = method.Invoke(null, [configuration]);
            return result as Template.Common.Models.Jwt.JwtSettingDto
                ?? throw new AssertFailedException("GetRequiredJwtCoreSettings result type is unexpected.");
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
