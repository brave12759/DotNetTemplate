using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.LoginService.Services;
using Template.BusinessRule.SsoService.Enums;
using Template.BusinessRule.SsoService.Exceptions;
using Template.BusinessRule.SsoService.Models;
using Template.BusinessRule.SsoService.Services;
using Template.Common.BackgroundQueue;
using Template.Common.Enums;
using Template.Common.Models;
using Template.Common.Services;
using Template.WebApi.Controllers;
using Template.WebApi.Models.Auth;

namespace Template.Test.Tests;

[TestClass]
public class AuthControllerTests
{
    [TestMethod]
    public async Task Login_Success_Should_ReturnOkToken()
    {
        var loginService = new FakeLoginService
        {
            LoginAsyncFunc = (_, _, _) => Task.FromResult(LoginResult.Ok("jwt-token"))
        };
        var controller = CreateAuthController(loginService, new CurrentUser());
        controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");

        var result = await controller.Login(new LoginRequest { UserId = "tester", Password = "pwd" });

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var payload = ok.Value as AuthTokenResponse;
        Assert.IsNotNull(payload);
        Assert.AreEqual("jwt-token", payload.Token);
    }

    [TestMethod]
    public async Task Login_AccountDisabled_Should_Return403()
    {
        var loginService = new FakeLoginService
        {
            LoginAsyncFunc = (_, _, _) => Task.FromResult(LoginResult.AccountLockedOut("disabled"))
        };
        var controller = CreateAuthController(loginService, new CurrentUser());

        var result = await controller.Login(new LoginRequest { UserId = "tester", Password = "pwd" });

        var obj = result as ObjectResult;
        Assert.IsNotNull(obj);
        Assert.AreEqual(StatusCodes.Status403Forbidden, obj.StatusCode);
        Assert.AreEqual("disabled", obj.Value);
    }

    [TestMethod]
    public async Task Refresh_MissingTokenInfo_Should_ReturnBadRequest()
    {
        var controller = CreateAuthController(new FakeLoginService(), new CurrentUser { UserId = string.Empty });

        var result = await controller.Refresh();

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Refresh_Failed_Should_ReturnUnauthorized_WhenNormalFailure()
    {
        var loginService = new FakeLoginService
        {
            RefreshAsyncFunc = (_, _, _, _) => Task.FromResult(LoginResult.Fail("invalid"))
        };
        var controller = CreateAuthController(loginService, new CurrentUser
        {
            UserId = "tester",
            TokenId = "jti-1",
            ExpiredTime = 1700001000
        });

        var result = await controller.Refresh();

        var unauthorized = result as UnauthorizedObjectResult;
        Assert.IsNotNull(unauthorized);
        Assert.AreEqual("invalid", unauthorized.Value);
    }

    [TestMethod]
    public async Task Logout_MissingBearer_Should_ReturnBadRequest()
    {
        var controller = CreateAuthController(new FakeLoginService(), new CurrentUser { TokenId = "j1", ExpiredTime = 1 });

        var result = await controller.Logout();

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Logout_Success_Should_CallServiceAndReturnOk()
    {
        var loginService = new FakeLoginService();
        var controller = CreateAuthController(loginService, new CurrentUser { TokenId = "jti-2", ExpiredTime = 1700000000 });
        controller.ControllerContext.HttpContext.Request.Headers.Authorization = "Bearer token";

        var result = await controller.Logout();

        Assert.IsNotNull(loginService.LastLogoutTokenId);
        Assert.AreEqual("jti-2", loginService.LastLogoutTokenId);
        Assert.AreEqual(1700000000, loginService.LastLogoutExpiredTime);
        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    private static AuthController CreateAuthController(FakeLoginService loginService, CurrentUser currentUser)
    {
        var controller = new AuthController(
            NullLogger<AuthController>.Instance,
            loginService,
            new FixedCurrentUserService(currentUser));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private sealed class FakeLoginService : ILoginService
    {
        public Func<string, string, string, Task<LoginResult>>? LoginAsyncFunc { get; set; }
        public Func<string, string, long, string, Task<LoginResult>>? RefreshAsyncFunc { get; set; }
        public string? LastLogoutTokenId { get; private set; }
        public long LastLogoutExpiredTime { get; private set; }

        public Task<LoginResult> LoginAsync(string userId, string password, string ip)
            => LoginAsyncFunc?.Invoke(userId, password, ip) ?? Task.FromResult(LoginResult.Fail("not configured"));

        public Task<LoginResult> DevLoginAsync(string userId, string ip)
            => Task.FromResult(LoginResult.Ok("dev"));

        public Task<LoginResult> RefreshAsync(string userId, string tokenId, long expiredUnixTimeSeconds, string ip)
            => RefreshAsyncFunc?.Invoke(userId, tokenId, expiredUnixTimeSeconds, ip) ?? Task.FromResult(LoginResult.Fail("not configured"));

        public Task LogoutAsync(string tokenId, long expiredUnixTimeSeconds)
        {
            LastLogoutTokenId = tokenId;
            LastLogoutExpiredTime = expiredUnixTimeSeconds;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedCurrentUserService(CurrentUser currentUser) : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = currentUser;
    }
}

[TestClass]
public class SsoControllerTests
{
    [TestMethod]
    public async Task Clients_Should_ReturnOk()
    {
        var ssoService = new FakeSsoService
        {
            GetClientsAsyncFunc = (_, _) => Task.FromResult(new PageListOutput<SsoClientDto>
            {
                TotalCount = 1,
                Items = [new SsoClientDto { ClientId = "erp" }]
            })
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.Clients("erp", true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task Login_Failed_Should_ReturnUnauthorizedWithCodePayload()
    {
        var ssoService = new FakeSsoService
        {
            LoginAsyncFunc = (_, _, _) => Task.FromResult(SsoTokenResult.Fail(SsoMessageEnum.InvalidClientCredentials))
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.Login(new SsoLoginRequest { ClientId = "a", ClientSecret = "b" });

        var unauthorized = result as UnauthorizedObjectResult;
        Assert.IsNotNull(unauthorized);
        var json = JsonSerializer.Serialize(unauthorized.Value);
        StringAssert.Contains(json, "InvalidClientCredentials");
    }

    [TestMethod]
    public async Task CreateClient_SsoMessageException_Should_ReturnBadRequest()
    {
        var ssoService = new FakeSsoService
        {
            CreateClientAsyncFunc = _ => throw new SsoMessageException(SsoMessageEnum.ClientIdRequired)
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.CreateClient(new SsoClientCreateRequest());

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        var json = JsonSerializer.Serialize(badRequest.Value);
        StringAssert.Contains(json, "ClientIdRequired");
    }

    [TestMethod]
    public async Task UpdateClient_NotFound_Should_ReturnNotFound()
    {
        var ssoService = new FakeSsoService
        {
            UpdateClientAsyncFunc = _ => Task.FromResult(false)
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.UpdateClient(new SsoClientUpdateRequest { Id = 9 });

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task ValidateToken_Should_ReturnOk()
    {
        var ssoService = new FakeSsoService
        {
            ValidateTokenAsyncFunc = _ => Task.FromResult(new SsoTokenValidateResult
            {
                IsValid = true,
                ClientId = "erp"
            })
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.ValidateToken(new TokenValidateRequest { Token = "t" });

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var payload = ok.Value as SsoTokenValidateResult;
        Assert.IsNotNull(payload);
        Assert.IsTrue(payload.IsValid);
    }

    [TestMethod]
    public async Task Refresh_Success_Should_ReturnOkToken()
    {
        var ssoService = new FakeSsoService
        {
            RefreshAsyncFunc = (_, _) => Task.FromResult(SsoTokenResult.Ok("refreshed-token"))
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.Refresh(new TokenValidateRequest { Token = "expired-token" });

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var json = JsonSerializer.Serialize(ok.Value);
        StringAssert.Contains(json, "refreshed-token");
    }

    [TestMethod]
    public async Task Login_Success_Should_ReturnOkToken()
    {
        var ssoService = new FakeSsoService
        {
            LoginAsyncFunc = (_, _, _) => Task.FromResult(SsoTokenResult.Ok("token-1"))
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.Login(new SsoLoginRequest { ClientId = "a", ClientSecret = "b" });

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var json = JsonSerializer.Serialize(ok.Value);
        StringAssert.Contains(json, "token-1");
    }

    [TestMethod]
    public async Task DeleteClient_NotFound_Should_ReturnNotFound()
    {
        var ssoService = new FakeSsoService
        {
            DeleteClientAsyncFunc = _ => Task.FromResult(false)
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.DeleteClient(1);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task DeleteClient_SsoMessageException_Should_ReturnBadRequest()
    {
        var ssoService = new FakeSsoService
        {
            DeleteClientAsyncFunc = _ => throw new SsoMessageException(SsoMessageEnum.IdMustBeGreaterThanZero)
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.DeleteClient(0);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task UpdateClient_Success_Should_ReturnOk()
    {
        var ssoService = new FakeSsoService
        {
            UpdateClientAsyncFunc = _ => Task.FromResult(true)
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.UpdateClient(new SsoClientUpdateRequest { Id = 1, ClientName = "c", IsEnable = true });

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task CreateClient_ArgumentException_Should_ReturnBadRequest()
    {
        var ssoService = new FakeSsoService
        {
            CreateClientAsyncFunc = _ => throw new ArgumentException("bad")
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.CreateClient(new SsoClientCreateRequest());

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task CreateClient_Success_Should_ReturnOk()
    {
        var ssoService = new FakeSsoService
        {
            CreateClientAsyncFunc = _ => Task.FromResult(new SsoClientDto { Id = 1, ClientId = "erp", ClientName = "ERP" })
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.CreateClient(new SsoClientCreateRequest { ClientId = "erp", ClientName = "ERP", ClientSecret = "s" });

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task UpdateClient_SsoMessageException_Should_ReturnBadRequest()
    {
        var ssoService = new FakeSsoService
        {
            UpdateClientAsyncFunc = _ => throw new SsoMessageException(SsoMessageEnum.ClientNameRequired)
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.UpdateClient(new SsoClientUpdateRequest { Id = 1 });

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task DeleteClient_Success_Should_ReturnOk()
    {
        var ssoService = new FakeSsoService
        {
            DeleteClientAsyncFunc = _ => Task.FromResult(true)
        };
        var controller = CreateSsoController(ssoService);

        var result = await controller.DeleteClient(1);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    private static SsoController CreateSsoController(FakeSsoService ssoService)
    {
        var controller = new SsoController(NullLogger<SsoController>.Instance, ssoService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return controller;
    }

    private sealed class FakeSsoService : ISsoService
    {
        public Func<string?, bool?, Task<PageListOutput<SsoClientDto>>>? GetClientsAsyncFunc { get; set; }
        public Func<SsoClientCreateRequest, Task<SsoClientDto>>? CreateClientAsyncFunc { get; set; }
        public Func<SsoClientUpdateRequest, Task<bool>>? UpdateClientAsyncFunc { get; set; }
        public Func<int, Task<bool>>? DeleteClientAsyncFunc { get; set; }
        public Func<string, string, string, Task<SsoTokenResult>>? LoginAsyncFunc { get; set; }
        public Func<string, string, Task<SsoTokenResult>>? RefreshAsyncFunc { get; set; }
        public Func<string, Task<SsoTokenValidateResult>>? ValidateTokenAsyncFunc { get; set; }

        public Task<PageListOutput<SsoClientDto>> GetClientsAsync(string? keyword, bool? isEnable, bool enablePaging = false, int page = 1, int pageSize = 50)
            => GetClientsAsyncFunc?.Invoke(keyword, isEnable) ?? Task.FromResult(new PageListOutput<SsoClientDto>());

        public Task<SsoClientDto> CreateClientAsync(SsoClientCreateRequest request)
            => CreateClientAsyncFunc?.Invoke(request) ?? Task.FromResult(new SsoClientDto());

        public Task<bool> UpdateClientAsync(SsoClientUpdateRequest request)
            => UpdateClientAsyncFunc?.Invoke(request) ?? Task.FromResult(true);

        public Task<bool> DeleteClientAsync(int id)
            => DeleteClientAsyncFunc?.Invoke(id) ?? Task.FromResult(true);

        public Task<SsoTokenResult> LoginAsync(string clientId, string clientSecret, string ip)
            => LoginAsyncFunc?.Invoke(clientId, clientSecret, ip) ?? Task.FromResult(SsoTokenResult.Ok("token"));

        public Task<SsoTokenResult> RefreshAsync(string token, string ip)
            => RefreshAsyncFunc?.Invoke(token, ip) ?? Task.FromResult(SsoTokenResult.Ok("token"));

        public Task<SsoTokenValidateResult> ValidateTokenAsync(string token)
            => ValidateTokenAsyncFunc?.Invoke(token) ?? Task.FromResult(new SsoTokenValidateResult());
    }
}

[TestClass]
public class LogControllerTests
{
    [TestMethod]
    public async Task UserOperationLogs_Success_Should_ReturnOk()
    {
        var logService = new FakeLogService
        {
            UserOperationQueryFunc = _ => Task.FromResult(new UserOperationLogQueryResult
            {
                TotalCount = 1,
                Page = 1,
                PageSize = 50
            })
        };
        var controller = new LogController(NullLogger<LogController>.Instance, logService);

        var result = await controller.UserOperationLogs(
            startTime: null,
            endTime: null,
            userId: "u",
            module: "m",
            action: null,
            result: null,
            targetType: null,
            targetId: null,
            page: 1,
            pageSize: 50);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task UserOperationLogs_ArgumentException_Should_ReturnBadRequest()
    {
        var logService = new FakeLogService
        {
            UserOperationQueryFunc = _ => throw new ArgumentException("bad page")
        };
        var controller = new LogController(NullLogger<LogController>.Instance, logService);

        var result = await controller.UserOperationLogs(
            startTime: null,
            endTime: null,
            userId: null,
            module: null,
            action: null,
            result: null,
            targetType: null,
            targetId: null,
            page: 0,
            pageSize: 50);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("bad page", badRequest.Value);
    }

    [TestMethod]
    public async Task QueueLogs_Success_Should_ReturnOk()
    {
        var logService = new FakeLogService
        {
            QueueLogQueryFunc = _ => Task.FromResult(new QueueLogQueryResult
            {
                TotalCount = 1,
                Page = 1,
                PageSize = 50
            })
        };
        var controller = new LogController(NullLogger<LogController>.Instance, logService);

        var result = await controller.QueueLogs(
            startTime: null,
            endTime: null,
            operatorId: null,
            page: 1,
            pageSize: 50);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var payload = ok.Value as QueueLogQueryResult;
        Assert.IsNotNull(payload);
        Assert.AreEqual(1, payload.TotalCount);
    }

    [TestMethod]
    public async Task SsoLogs_ArgumentException_Should_ReturnBadRequest()
    {
        var logService = new FakeLogService
        {
            SsoLogQueryFunc = _ => throw new ArgumentException("bad query")
        };
        var controller = new LogController(NullLogger<LogController>.Instance, logService);

        var result = await controller.SsoLogs(null, null, null, 0, 50);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task SsoLogs_Success_Should_ReturnOk()
    {
        var logService = new FakeLogService
        {
            SsoLogQueryFunc = _ => Task.FromResult(new SsoLogQueryResult
            {
                TotalCount = 2,
                Page = 1,
                PageSize = 50
            })
        };
        var controller = new LogController(NullLogger<LogController>.Instance, logService);

        var result = await controller.SsoLogs(null, null, "client", 1, 50);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var payload = ok.Value as SsoLogQueryResult;
        Assert.IsNotNull(payload);
        Assert.AreEqual(2, payload.TotalCount);
    }

    private sealed class FakeLogService : ILogService
    {
        public Func<UserOperationLogQueryRequest, Task<UserOperationLogQueryResult>>? UserOperationQueryFunc { get; set; }
        public Func<QueueLogQueryRequest, Task<QueueLogQueryResult>>? QueueLogQueryFunc { get; set; }
        public Func<SsoLogQueryRequest, Task<SsoLogQueryResult>>? SsoLogQueryFunc { get; set; }

        public Task<long> WriteUserOperationAsync(UserOperationLogCreateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(1L);

        public Task<UserOperationLogQueryResult> GetUserOperationLogsAsync(UserOperationLogQueryRequest request, CancellationToken cancellationToken = default)
            => UserOperationQueryFunc?.Invoke(request) ?? Task.FromResult(new UserOperationLogQueryResult());

        public Task<long> WriteQueueAsync(QueueLogCreateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(1L);

        public Task<QueueLogQueryResult> GetQueueLogsAsync(QueueLogQueryRequest request, CancellationToken cancellationToken = default)
            => QueueLogQueryFunc?.Invoke(request) ?? Task.FromResult(new QueueLogQueryResult());

        public Task<long> WriteSsoAsync(SsoLogCreateRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(1L);

        public Task<SsoLogQueryResult> GetSsoLogsAsync(SsoLogQueryRequest request, CancellationToken cancellationToken = default)
            => SsoLogQueryFunc?.Invoke(request) ?? Task.FromResult(new SsoLogQueryResult());
    }
}

[TestClass]
public class BackgroundQueueControllerTests
{
    [TestMethod]
    public async Task List_InvalidPaging_Should_ReturnBadRequest()
    {
        var monitor = new FakeBackgroundJobMonitorService
        {
            ListFunc = (_, _, _, _, _) => throw new ArgumentException("invalid paging")
        };
        var controller = new BackgroundQueueController(NullLogger<BackgroundQueueController>.Instance, monitor);

        var result = await controller.List(
            workType: null,
            status: null,
            page: 0,
            pageSize: 500,
            cancellationToken: CancellationToken.None);

        var badRequest = result as BadRequestObjectResult;
        Assert.IsNotNull(badRequest);
        Assert.AreEqual("invalid paging", badRequest.Value);
    }

    [TestMethod]
    public async Task GetById_NotFound_Should_ReturnNotFound()
    {
        var monitor = new FakeBackgroundJobMonitorService
        {
            GetByIdFunc = (_, _) => Task.FromResult<BackgroundJobDto?>(null)
        };
        var controller = new BackgroundQueueController(NullLogger<BackgroundQueueController>.Instance, monitor);

        var result = await controller.GetById(99, CancellationToken.None);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Summary_Should_ReturnOk()
    {
        var monitor = new FakeBackgroundJobMonitorService
        {
            SummaryFunc = _ => Task.FromResult(new BackgroundJobSummaryDto { PendingCount = 3 })
        };
        var controller = new BackgroundQueueController(NullLogger<BackgroundQueueController>.Instance, monitor);

        var result = await controller.Summary(CancellationToken.None);

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var payload = ok.Value as BackgroundJobSummaryDto;
        Assert.IsNotNull(payload);
        Assert.AreEqual(3, payload.PendingCount);
    }

    private sealed class FakeBackgroundJobMonitorService : IBackgroundJobMonitorService
    {
        public Func<CancellationToken, Task<BackgroundJobSummaryDto>>? SummaryFunc { get; set; }
        public Func<BackgroundWorkType?, BackgroundJobStatus?, int, int, CancellationToken, Task<BackgroundJobQueryResult>>? ListFunc { get; set; }
        public Func<long, CancellationToken, Task<BackgroundJobDto?>>? GetByIdFunc { get; set; }

        public Task<BackgroundJobSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
            => SummaryFunc?.Invoke(cancellationToken) ?? Task.FromResult(new BackgroundJobSummaryDto());

        public Task<BackgroundJobQueryResult> GetListAsync(BackgroundWorkType? workType = null, BackgroundJobStatus? status = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
            => ListFunc?.Invoke(workType, status, page, pageSize, cancellationToken) ?? Task.FromResult(new BackgroundJobQueryResult());

        public Task<BackgroundJobDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => GetByIdFunc?.Invoke(id, cancellationToken) ?? Task.FromResult<BackgroundJobDto?>(new BackgroundJobDto());
    }
}
