using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.CryptographyService.Models;
using Template.BusinessRule.CryptographyService.Services;
using Template.BusinessRule.DepartmentService.Models;
using Template.BusinessRule.DepartmentService.Services;
using Template.BusinessRule.FunctionPermissionService.Models;
using Template.BusinessRule.FunctionPermissionService.Services;
using Template.BusinessRule.MenuTreeService.Models;
using Template.BusinessRule.MenuTreeService.Services;
using Template.BusinessRule.RoleGroupService.Models;
using Template.BusinessRule.RoleGroupService.Services;
using Template.BusinessRule.UserService.Services;
using Template.Common.Models;
using Template.Common.Models.Jwt;
using Template.Common.Models.User;
using Template.Common.Services;
using Template.WebApi.Controllers;

namespace Template.Test.Tests;

[TestClass]
public class DepartmentControllerTests
{
    [TestMethod]
    public async Task List_Should_ReturnOk()
    {
        var service = new FakeDepartmentService();
        var controller = new DepartmentController(NullLogger<DepartmentController>.Instance, service);

        var result = await controller.List("IT", true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task GetById_InvalidId_Should_ReturnBadRequest()
    {
        var service = new FakeDepartmentService { GetByIdAsyncFunc = _ => throw new ArgumentException("bad") };
        var controller = new DepartmentController(NullLogger<DepartmentController>.Instance, service);

        var result = await controller.GetById(0);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Update_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeDepartmentService { UpdateAsyncFunc = _ => Task.FromResult(false) };
        var controller = new DepartmentController(NullLogger<DepartmentController>.Instance, service);

        var result = await controller.Update(new DepartmentUpdateRequest { DeptId = 1, DeptName = "A" });

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Delete_InvalidOperation_Should_ReturnBadRequest()
    {
        var service = new FakeDepartmentService { DeleteAsyncFunc = _ => throw new InvalidOperationException("has children") };
        var controller = new DepartmentController(NullLogger<DepartmentController>.Instance, service);

        var result = await controller.Delete(1);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Tree_Should_ReturnOk()
    {
        var service = new FakeDepartmentService();
        var controller = new DepartmentController(NullLogger<DepartmentController>.Instance, service);

        var result = await controller.Tree(true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task Create_Should_ReturnOk()
    {
        var service = new FakeDepartmentService();
        var controller = new DepartmentController(NullLogger<DepartmentController>.Instance, service);

        var result = await controller.Create(new DepartmentCreateRequest { DeptName = "A", ParentDeptId = null, SortOrder = 1, IsEnable = true });

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task Delete_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeDepartmentService { DeleteAsyncFunc = _ => Task.FromResult(false) };
        var controller = new DepartmentController(NullLogger<DepartmentController>.Instance, service);

        var result = await controller.Delete(999);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }
}

[TestClass]
public class UserControllerTests
{
    [TestMethod]
    public async Task List_Should_ReturnOk()
    {
        var service = new FakeUserService();
        var controller = new UserController(NullLogger<UserController>.Instance, service);

        var result = await controller.List("k", true, 1, includeSubDepartments: true, enablePaging: true, page: 1, pageSize: 10);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task GetById_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeUserService { GetByIdAsyncFunc = _ => Task.FromResult<UserDto?>(null) };
        var controller = new UserController(NullLogger<UserController>.Instance, service);

        var result = await controller.GetById(1);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Create_InvalidOperation_Should_Return500()
    {
        var service = new FakeUserService { CreateAsyncFunc = _ => throw new InvalidOperationException("db down") };
        var controller = new UserController(NullLogger<UserController>.Instance, service);

        var result = await controller.Create(new UserCreateRequest { UserId = "u1", UserName = "U1", Password = "P@ssw0rd", DeptId = 1 });

        var obj = result as ObjectResult;
        Assert.IsNotNull(obj);
        Assert.AreEqual(StatusCodes.Status500InternalServerError, obj.StatusCode);
    }

    [TestMethod]
    public async Task ChangePassword_UnauthorizedAccess_Should_Return401()
    {
        var service = new FakeUserService { ChangePasswordAsyncFunc = _ => throw new UnauthorizedAccessException() };
        var controller = new UserController(NullLogger<UserController>.Instance, service);

        var result = await controller.ChangePassword(new UserChangePasswordRequest
        {
            Id = 1,
            OldPassword = "old",
            NewPassword = "new"
        });

        Assert.IsInstanceOfType<UnauthorizedObjectResult>(result);
    }

    [TestMethod]
    public async Task Update_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeUserService { UpdateAsyncFunc = _ => Task.FromResult(false) };
        var controller = new UserController(NullLogger<UserController>.Instance, service);

        var result = await controller.Update(new UserUpdateRequest { Id = 1, UserName = "A", DeptId = 1, IsEnable = true });

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Delete_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeUserService { DeleteAsyncFunc = _ => Task.FromResult(false) };
        var controller = new UserController(NullLogger<UserController>.Instance, service);

        var result = await controller.Delete(1);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task ResetPassword_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeUserService { ResetPasswordAsyncFunc = _ => Task.FromResult(false) };
        var controller = new UserController(NullLogger<UserController>.Instance, service);

        var result = await controller.ResetPassword(new UserResetPasswordRequest { Id = 1, NewPassword = "new" });

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Create_ArgumentException_Should_ReturnBadRequest()
    {
        var service = new FakeUserService { CreateAsyncFunc = _ => throw new ArgumentException("bad") };
        var controller = new UserController(NullLogger<UserController>.Instance, service);

        var result = await controller.Create(new UserCreateRequest { UserId = "u", UserName = "u", Password = "P@ssw0rd", DeptId = 1 });

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }
}

[TestClass]
public class FunctionPermissionControllerTests
{
    [TestMethod]
    public async Task List_Should_ReturnOk()
    {
        var service = new FakeFunctionPermissionService();
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        var result = await controller.List("k", true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task Tree_Should_ReturnOk()
    {
        var service = new FakeFunctionPermissionService();
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        var result = await controller.Tree(true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task GetById_InvalidId_Should_ReturnBadRequest()
    {
        var service = new FakeFunctionPermissionService { GetByIdAsyncFunc = _ => throw new ArgumentException("bad") };
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        var result = await controller.GetById(0);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task UpdateRoleGroup_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeFunctionPermissionService { UpdateRoleGroupPermissionsAsyncFunc = _ => Task.FromResult(false) };
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        var result = await controller.UpdateRoleGroup(new RoleGroupFunctionPermissionUpdateRequest { RoleGroupId = 2 });

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Delete_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeFunctionPermissionService { DeleteAsyncFunc = _ => Task.FromResult(false) };
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        var result = await controller.Delete(99);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task SyncFromMenuTree_Should_ReturnOk()
    {
        var service = new FakeFunctionPermissionService
        {
            SyncFromMenuTreeAsyncFunc = _ => Task.FromResult(new FunctionPermissionSyncResult { CreatedFunctionCount = 3 })
        };
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        var result = await controller.SyncFromMenuTree();

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var payload = ok.Value as FunctionPermissionSyncResult;
        Assert.IsNotNull(payload);
        Assert.AreEqual(3, payload.CreatedFunctionCount);
    }

    [TestMethod]
    public async Task GetRoleGroupPermissions_ArgumentException_Should_ReturnBadRequest()
    {
        var service = new FakeFunctionPermissionService
        {
            GetRoleGroupPermissionsAsyncFunc = (_, _) => throw new ArgumentException("bad")
        };
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        var result = await controller.GetRoleGroupPermissions(1, true);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task RoleGroupTree_Should_ReturnOk()
    {
        var service = new FakeFunctionPermissionService();
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        var result = await controller.RoleGroupTree(1, true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task UserTree_Should_ReturnOk()
    {
        var service = new FakeFunctionPermissionService();
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        var result = await controller.UserTree("u1", true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }
}

[TestClass]
public class MenuTreeControllerTests
{
    [TestMethod]
    public async Task List_Should_ReturnOk()
    {
        var service = new FakeMenuTreeService();
        var controller = new MenuTreeController(NullLogger<MenuTreeController>.Instance, service);

        var result = await controller.List("k", true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task Tree_Should_ReturnOk()
    {
        var service = new FakeMenuTreeService();
        var controller = new MenuTreeController(NullLogger<MenuTreeController>.Instance, service);

        var result = await controller.Tree(true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task GetById_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeMenuTreeService { GetByIdAsyncFunc = _ => Task.FromResult<MenuTreeDto?>(null) };
        var controller = new MenuTreeController(NullLogger<MenuTreeController>.Instance, service);

        var result = await controller.GetById(999);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Delete_InvalidOperation_Should_ReturnBadRequest()
    {
        var service = new FakeMenuTreeService { DeleteAsyncFunc = _ => throw new InvalidOperationException("has children") };
        var controller = new MenuTreeController(NullLogger<MenuTreeController>.Instance, service);

        var result = await controller.Delete(1);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Update_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeMenuTreeService { UpdateAsyncFunc = _ => Task.FromResult(false) };
        var controller = new MenuTreeController(NullLogger<MenuTreeController>.Instance, service);

        var result = await controller.Update(new MenuTreeUpdateRequest { Id = 1, MenuCode = "A", MenuName = "B" });

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Create_Should_ReturnOk()
    {
        var service = new FakeMenuTreeService();
        var controller = new MenuTreeController(NullLogger<MenuTreeController>.Instance, service);

        var result = await controller.Create(new MenuTreeCreateRequest { MenuCode = "A", MenuName = "A", SortOrder = 1, IsEnable = true });

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }
}

[TestClass]
public class RoleGroupControllerTests
{
    [TestMethod]
    public async Task List_Should_ReturnOk()
    {
        var service = new FakeRoleGroupService();
        var controller = new RoleGroupController(NullLogger<RoleGroupController>.Instance, service);

        var result = await controller.List("k", true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task Tree_Should_ReturnOk()
    {
        var service = new FakeRoleGroupService();
        var controller = new RoleGroupController(NullLogger<RoleGroupController>.Instance, service);

        var result = await controller.Tree(true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }

    [TestMethod]
    public async Task UpdateUser_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeRoleGroupService { UpdateUserRoleGroupsAsyncFunc = _ => Task.FromResult(false) };
        var controller = new RoleGroupController(NullLogger<RoleGroupController>.Instance, service);

        var result = await controller.UpdateUser(new UserRoleGroupUpdateRequest { UserId = "u1" });

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task Delete_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeRoleGroupService { DeleteAsyncFunc = _ => Task.FromResult(false) };
        var controller = new RoleGroupController(NullLogger<RoleGroupController>.Instance, service);

        var result = await controller.Delete(3);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task GetById_NotFound_Should_ReturnNotFound()
    {
        var service = new FakeRoleGroupService { GetByIdAsyncFunc = _ => Task.FromResult<RoleGroupDto?>(null) };
        var controller = new RoleGroupController(NullLogger<RoleGroupController>.Instance, service);

        var result = await controller.GetById(3);

        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
    }

    [TestMethod]
    public async Task GetUserRoleGroups_Should_ReturnOk()
    {
        var service = new FakeRoleGroupService();
        var controller = new RoleGroupController(NullLogger<RoleGroupController>.Instance, service);

        var result = await controller.GetUserRoleGroups("u1", true);

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }
}

[TestClass]
public class JwtSettingControllerTests
{
    [TestMethod]
    public async Task Get_Should_MaskSecretKey()
    {
        var jwt = new FakeJwtService
        {
            GetSettingsAsyncFunc = () => Task.FromResult(new JwtSettingDto
            {
                SecretKey = "1234567890ABCDEFGH",
                Issuer = "iss",
                Audience = "aud",
                PersonalTokenExpire = 30,
                ServerTokenExpire = 60
            })
        };
        var controller = new JwtSettingController(
            NullLogger<JwtSettingController>.Instance,
            jwt,
            new WebApiFixedCurrentUserService(new CurrentUser { UserId = "admin" }));

        var result = await controller.Get();

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        StringAssert.Contains(json, "1234");
        StringAssert.Contains(json, "EFGH");
        StringAssert.Contains(json, "****");
    }

    [TestMethod]
    public async Task Update_ArgumentException_Should_ReturnBadRequest()
    {
        var jwt = new FakeJwtService
        {
            UpdateSettingsAsyncFunc = (_, _) => throw new ArgumentException("bad")
        };
        var controller = new JwtSettingController(
            NullLogger<JwtSettingController>.Instance,
            jwt,
            new WebApiFixedCurrentUserService(new CurrentUser { UserId = "admin" }));

        var result = await controller.Update(new JwtSettingUpdateRequest());

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public async Task Update_Success_Should_ReturnOk()
    {
        var jwt = new FakeJwtService();
        var controller = new JwtSettingController(
            NullLogger<JwtSettingController>.Instance,
            jwt,
            new WebApiFixedCurrentUserService(new CurrentUser { UserId = "admin" }));

        var result = await controller.Update(new JwtSettingUpdateRequest
        {
            PersonalTokenExpire = 30,
            ServerTokenExpire = 60
        });

        Assert.IsInstanceOfType<OkObjectResult>(result);
    }
}

[TestClass]
public class CryptographyControllerTests
{
    [TestMethod]
    public void GenerateSymmetricKey_InvalidBits_Should_ReturnBadRequest()
    {
        var crypto = new FakeCryptographyService
        {
            GenerateSymmetricKeyFunc = _ => throw new ArgumentException("bits")
        };
        var controller = new CryptographyController(NullLogger<CryptographyController>.Instance, crypto);

        var result = controller.GenerateSymmetricKey(111);

        Assert.IsInstanceOfType<BadRequestObjectResult>(result);
    }

    [TestMethod]
    public void VerifyHash_Success_Should_ReturnOk()
    {
        var crypto = new FakeCryptographyService
        {
            VerifyHashFunc = (_, _) => true
        };
        var controller = new CryptographyController(NullLogger<CryptographyController>.Instance, crypto);

        var result = controller.VerifyHash(new CryptographyServiceVerifyHashRequest
        {
            PlainText = "abc",
            HashValue = "hash"
        });

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        StringAssert.Contains(json, "true");
    }

    [TestMethod]
    public void GenerateRsaKeyPair_Success_Should_ReturnOk()
    {
        var controller = new CryptographyController(NullLogger<CryptographyController>.Instance, new FakeCryptographyService
        {
            GenerateRsaKeyPairFunc = _ => ("public", "private")
        });

        var result = controller.GenerateRsaKeyPair(new CryptographyServiceGenerateRsaKeyPairRequest { KeySizeBits = 2048 });

        var ok = result as OkObjectResult;
        Assert.IsNotNull(ok);
        var json = System.Text.Json.JsonSerializer.Serialize(ok.Value);
        StringAssert.Contains(json, "public");
    }
}

internal sealed class FakeDepartmentService : IDepartmentService
{
    public Func<string?, bool?, bool, int, int, Task<PageListOutput<DepartmentDto>>>? GetListAsyncFunc { get; set; }
    public Func<bool?, Task<IReadOnlyList<DepartmentDto>>>? GetTreeAsyncFunc { get; set; }
    public Func<int, Task<DepartmentDto?>>? GetByIdAsyncFunc { get; set; }
    public Func<DepartmentCreateRequest, Task<DepartmentDto>>? CreateAsyncFunc { get; set; }
    public Func<DepartmentUpdateRequest, Task<bool>>? UpdateAsyncFunc { get; set; }
    public Func<int, Task<bool>>? DeleteAsyncFunc { get; set; }

    public Task<PageListOutput<DepartmentDto>> GetListAsync(string? keyword, bool? isEnable, bool enablePaging = false, int page = 1, int pageSize = 50)
        => GetListAsyncFunc?.Invoke(keyword, isEnable, enablePaging, page, pageSize) ?? Task.FromResult(new PageListOutput<DepartmentDto>());

    public Task<IReadOnlyList<DepartmentDto>> GetTreeAsync(bool? isEnable)
        => GetTreeAsyncFunc?.Invoke(isEnable) ?? Task.FromResult<IReadOnlyList<DepartmentDto>>([]);

    public Task<DepartmentDto?> GetByIdAsync(int deptId)
        => GetByIdAsyncFunc?.Invoke(deptId) ?? Task.FromResult<DepartmentDto?>(null);

    public Task<DepartmentDto> CreateAsync(DepartmentCreateRequest request)
        => CreateAsyncFunc?.Invoke(request) ?? Task.FromResult(new DepartmentDto());

    public Task<bool> UpdateAsync(DepartmentUpdateRequest request)
        => UpdateAsyncFunc?.Invoke(request) ?? Task.FromResult(true);

    public Task<bool> DeleteAsync(int deptId)
        => DeleteAsyncFunc?.Invoke(deptId) ?? Task.FromResult(true);
}

internal sealed class FakeUserService : IUserService
{
    public Func<string?, bool?, int?, bool, bool, int, int, Task<PageListOutput<UserDto>>>? GetListAsyncFunc { get; set; }
    public Func<UserCreateRequest, Task<UserDto>>? CreateAsyncFunc { get; set; }
    public Func<UserChangePasswordRequest, Task<bool>>? ChangePasswordAsyncFunc { get; set; }
    public Func<int, Task<UserDto?>>? GetByIdAsyncFunc { get; set; }
    public Func<UserUpdateRequest, Task<bool>>? UpdateAsyncFunc { get; set; }
    public Func<int, Task<bool>>? DeleteAsyncFunc { get; set; }
    public Func<UserResetPasswordRequest, Task<bool>>? ResetPasswordAsyncFunc { get; set; }

    public Task<PageListOutput<UserDto>> GetListAsync(string? keyword, bool? isEnable, int? deptId = null, bool includeSubDepartments = false, bool enablePaging = false, int page = 1, int pageSize = 50)
        => GetListAsyncFunc?.Invoke(keyword, isEnable, deptId, includeSubDepartments, enablePaging, page, pageSize) ?? Task.FromResult(new PageListOutput<UserDto>());

    public Task<UserDto?> GetByIdAsync(int id) => GetByIdAsyncFunc?.Invoke(id) ?? Task.FromResult<UserDto?>(null);

    public Task<UserDto> CreateAsync(UserCreateRequest request)
        => CreateAsyncFunc?.Invoke(request) ?? Task.FromResult(new UserDto());

    public Task<bool> UpdateAsync(UserUpdateRequest request) => UpdateAsyncFunc?.Invoke(request) ?? Task.FromResult(true);

    public Task<bool> DeleteAsync(int id) => DeleteAsyncFunc?.Invoke(id) ?? Task.FromResult(true);

    public Task<bool> ResetPasswordAsync(UserResetPasswordRequest request) => ResetPasswordAsyncFunc?.Invoke(request) ?? Task.FromResult(true);

    public Task<bool> ChangePasswordAsync(UserChangePasswordRequest request)
        => ChangePasswordAsyncFunc?.Invoke(request) ?? Task.FromResult(true);
}

internal sealed class FakeFunctionPermissionService : IFunctionPermissionService
{
    public Func<string?, bool?, bool, int, int, Task<PageListOutput<FunctionPermissionDto>>>? GetListAsyncFunc { get; set; }
    public Func<bool?, Task<IReadOnlyList<FunctionPermissionDto>>>? GetTreeAsyncFunc { get; set; }
    public Func<int, Task<FunctionPermissionDto?>>? GetByIdAsyncFunc { get; set; }
    public Func<FunctionPermissionCreateRequest, Task<FunctionPermissionDto>>? CreateAsyncFunc { get; set; }
    public Func<FunctionPermissionUpdateRequest, Task<bool>>? UpdateAsyncFunc { get; set; }
    public Func<RoleGroupFunctionPermissionUpdateRequest, Task<bool>>? UpdateRoleGroupPermissionsAsyncFunc { get; set; }
    public Func<int, Task<bool>>? DeleteAsyncFunc { get; set; }
    public Func<bool, Task<FunctionPermissionSyncResult>>? SyncFromMenuTreeAsyncFunc { get; set; }
    public Func<int, bool?, Task<IReadOnlyList<FunctionPermissionDto>>>? GetRoleGroupPermissionsAsyncFunc { get; set; }
    public Func<int, bool?, Task<IReadOnlyList<FunctionPermissionDto>>>? GetRoleGroupPermissionTreeAsyncFunc { get; set; }
    public Func<string, bool?, Task<IReadOnlyList<FunctionPermissionDto>>>? GetUserPermissionTreeAsyncFunc { get; set; }

    public Task<PageListOutput<FunctionPermissionDto>> GetListAsync(string? keyword, bool? isEnable, bool enablePaging = false, int page = 1, int pageSize = 50)
        => GetListAsyncFunc?.Invoke(keyword, isEnable, enablePaging, page, pageSize) ?? Task.FromResult(new PageListOutput<FunctionPermissionDto>());

    public Task<IReadOnlyList<FunctionPermissionDto>> GetTreeAsync(bool? isEnable)
        => GetTreeAsyncFunc?.Invoke(isEnable) ?? Task.FromResult<IReadOnlyList<FunctionPermissionDto>>([]);

    public Task<FunctionPermissionDto?> GetByIdAsync(int functionPermissionId)
        => GetByIdAsyncFunc?.Invoke(functionPermissionId) ?? Task.FromResult<FunctionPermissionDto?>(null);

    public Task<FunctionPermissionDto> CreateAsync(FunctionPermissionCreateRequest request)
        => CreateAsyncFunc?.Invoke(request) ?? Task.FromResult(new FunctionPermissionDto());

    public Task<bool> UpdateAsync(FunctionPermissionUpdateRequest request)
        => UpdateAsyncFunc?.Invoke(request) ?? Task.FromResult(true);

    public Task<bool> DeleteAsync(int functionPermissionId)
        => DeleteAsyncFunc?.Invoke(functionPermissionId) ?? Task.FromResult(true);

    public Task<FunctionPermissionSyncResult> SyncFromMenuTreeAsync(bool includeDisabledMenus = false)
        => SyncFromMenuTreeAsyncFunc?.Invoke(includeDisabledMenus) ?? Task.FromResult(new FunctionPermissionSyncResult());

    public Task<IReadOnlyList<FunctionPermissionDto>> GetRoleGroupPermissionsAsync(int roleGroupId, bool? isEnable)
        => GetRoleGroupPermissionsAsyncFunc?.Invoke(roleGroupId, isEnable) ?? Task.FromResult<IReadOnlyList<FunctionPermissionDto>>([]);

    public Task<IReadOnlyList<FunctionPermissionDto>> GetRoleGroupPermissionTreeAsync(int roleGroupId, bool? isEnable)
        => GetRoleGroupPermissionTreeAsyncFunc?.Invoke(roleGroupId, isEnable) ?? Task.FromResult<IReadOnlyList<FunctionPermissionDto>>([]);

    public Task<bool> UpdateRoleGroupPermissionsAsync(RoleGroupFunctionPermissionUpdateRequest request)
        => UpdateRoleGroupPermissionsAsyncFunc?.Invoke(request) ?? Task.FromResult(true);

    public Task<IReadOnlyList<FunctionPermissionDto>> GetUserPermissionTreeAsync(string userId, bool? isEnable)
        => GetUserPermissionTreeAsyncFunc?.Invoke(userId, isEnable) ?? Task.FromResult<IReadOnlyList<FunctionPermissionDto>>([]);
}

internal sealed class FakeMenuTreeService : IMenuTreeService
{
    public Func<string?, bool?, bool, int, int, Task<PageListOutput<MenuTreeDto>>>? GetListAsyncFunc { get; set; }
    public Func<bool?, Task<IReadOnlyList<MenuTreeDto>>>? GetTreeAsyncFunc { get; set; }
    public Func<int, Task<bool>>? DeleteAsyncFunc { get; set; }
    public Func<int, Task<MenuTreeDto?>>? GetByIdAsyncFunc { get; set; }
    public Func<MenuTreeCreateRequest, Task<MenuTreeDto>>? CreateAsyncFunc { get; set; }
    public Func<MenuTreeUpdateRequest, Task<bool>>? UpdateAsyncFunc { get; set; }

    public Task<PageListOutput<MenuTreeDto>> GetListAsync(string? keyword, bool? isEnable, bool enablePaging = false, int page = 1, int pageSize = 50)
        => GetListAsyncFunc?.Invoke(keyword, isEnable, enablePaging, page, pageSize) ?? Task.FromResult(new PageListOutput<MenuTreeDto>());

    public Task<IReadOnlyList<MenuTreeDto>> GetTreeAsync(bool? isEnable)
        => GetTreeAsyncFunc?.Invoke(isEnable) ?? Task.FromResult<IReadOnlyList<MenuTreeDto>>([]);

    public Task<MenuTreeDto?> GetByIdAsync(int id) => GetByIdAsyncFunc?.Invoke(id) ?? Task.FromResult<MenuTreeDto?>(null);

    public Task<MenuTreeDto> CreateAsync(MenuTreeCreateRequest request) => CreateAsyncFunc?.Invoke(request) ?? Task.FromResult(new MenuTreeDto());

    public Task<bool> UpdateAsync(MenuTreeUpdateRequest request) => UpdateAsyncFunc?.Invoke(request) ?? Task.FromResult(true);

    public Task<bool> DeleteAsync(int id) => DeleteAsyncFunc?.Invoke(id) ?? Task.FromResult(true);
}

internal sealed class FakeRoleGroupService : IRoleGroupService
{
    public Func<string?, bool?, bool, int, int, Task<PageListOutput<RoleGroupDto>>>? GetListAsyncFunc { get; set; }
    public Func<bool?, Task<IReadOnlyList<RoleGroupDto>>>? GetTreeAsyncFunc { get; set; }
    public Func<UserRoleGroupUpdateRequest, Task<bool>>? UpdateUserRoleGroupsAsyncFunc { get; set; }
    public Func<int, Task<bool>>? DeleteAsyncFunc { get; set; }
    public Func<int, Task<RoleGroupDto?>>? GetByIdAsyncFunc { get; set; }
    public Func<RoleGroupCreateRequest, Task<RoleGroupDto>>? CreateAsyncFunc { get; set; }
    public Func<RoleGroupUpdateRequest, Task<bool>>? UpdateAsyncFunc { get; set; }
    public Func<string, bool?, Task<IReadOnlyList<RoleGroupDto>>>? GetUserRoleGroupsAsyncFunc { get; set; }

    public Task<PageListOutput<RoleGroupDto>> GetListAsync(string? keyword, bool? isEnable, bool enablePaging = false, int page = 1, int pageSize = 50)
        => GetListAsyncFunc?.Invoke(keyword, isEnable, enablePaging, page, pageSize) ?? Task.FromResult(new PageListOutput<RoleGroupDto>());

    public Task<IReadOnlyList<RoleGroupDto>> GetTreeAsync(bool? isEnable)
        => GetTreeAsyncFunc?.Invoke(isEnable) ?? Task.FromResult<IReadOnlyList<RoleGroupDto>>([]);

    public Task<RoleGroupDto?> GetByIdAsync(int roleGroupId) => GetByIdAsyncFunc?.Invoke(roleGroupId) ?? Task.FromResult<RoleGroupDto?>(null);

    public Task<RoleGroupDto> CreateAsync(RoleGroupCreateRequest request) => CreateAsyncFunc?.Invoke(request) ?? Task.FromResult(new RoleGroupDto());

    public Task<bool> UpdateAsync(RoleGroupUpdateRequest request) => UpdateAsyncFunc?.Invoke(request) ?? Task.FromResult(true);

    public Task<bool> DeleteAsync(int roleGroupId) => DeleteAsyncFunc?.Invoke(roleGroupId) ?? Task.FromResult(true);

    public Task<IReadOnlyList<RoleGroupDto>> GetUserRoleGroupsAsync(string userId, bool? isEnable)
        => GetUserRoleGroupsAsyncFunc?.Invoke(userId, isEnable) ?? Task.FromResult<IReadOnlyList<RoleGroupDto>>([]);

    public Task<bool> UpdateUserRoleGroupsAsync(UserRoleGroupUpdateRequest request)
        => UpdateUserRoleGroupsAsyncFunc?.Invoke(request) ?? Task.FromResult(true);
}

internal sealed class FakeJwtService : IJwtService
{
    public Func<Task<JwtSettingDto>>? GetSettingsAsyncFunc { get; set; }
    public Action<JwtSettingUpdateRequest, string>? UpdateSettingsAsyncFunc { get; set; }

    public Task<string> GeneratePersonalTokenAsync(string userId, string email, string mobilePhone, string deptId, string ip, string? roleGroupsJson = null, string? functionPermissionsJson = null)
        => Task.FromResult("token");

    public Task<string> GenerateServerTokenAsync(string clientId, string ip)
        => Task.FromResult("server-token");

    public Task<System.Security.Claims.ClaimsPrincipal?> ValidateTokenAsync(string token, bool validateRevocation = true)
        => Task.FromResult<System.Security.Claims.ClaimsPrincipal?>(null);

    public Task<System.Security.Claims.ClaimsPrincipal?> ValidateExpiredTokenAsync(string token)
        => Task.FromResult<System.Security.Claims.ClaimsPrincipal?>(null);

    public Task<JwtSettingDto> GetSettingsAsync()
        => GetSettingsAsyncFunc?.Invoke() ?? Task.FromResult(new JwtSettingDto());

    public Task UpdateSettingsAsync(JwtSettingUpdateRequest request, string updatedBy)
    {
        UpdateSettingsAsyncFunc?.Invoke(request, updatedBy);
        return Task.CompletedTask;
    }
}

internal sealed class FakeCryptographyService : ICryptographyService
{
    public Func<int, (string KeyBase64, string IvBase64)>? GenerateSymmetricKeyFunc { get; set; }
    public Func<string, string>? SymmetricEncryptFunc { get; set; }
    public Func<string, string>? SymmetricDecryptFunc { get; set; }
    public Func<string, string, bool>? VerifyHashFunc { get; set; }
    public Func<int, (string PublicKeyPem, string PrivateKeyPem)>? GenerateRsaKeyPairFunc { get; set; }
    public Func<string, string>? AsymmetricEncryptFunc { get; set; }
    public Func<string, string>? AsymmetricDecryptFunc { get; set; }
    public Func<string, string>? SignFunc { get; set; }
    public Func<string, string, bool>? VerifySignatureFunc { get; set; }
    public Func<string, string>? HashFunc { get; set; }

    public (string KeyBase64, string IvBase64) GenerateSymmetricKey(int keySizeBits = 256)
        => GenerateSymmetricKeyFunc?.Invoke(keySizeBits) ?? ("k", "i");

    public string SymmetricEncrypt(string plainText) => SymmetricEncryptFunc?.Invoke(plainText) ?? "cipher";
    public string SymmetricDecrypt(string cipherTextBase64) => SymmetricDecryptFunc?.Invoke(cipherTextBase64) ?? "plain";
    public (string PublicKeyPem, string PrivateKeyPem) GenerateRsaKeyPair(int keySizeBits = 2048) => GenerateRsaKeyPairFunc?.Invoke(keySizeBits) ?? ("pub", "pri");
    public string AsymmetricEncrypt(string plainText) => AsymmetricEncryptFunc?.Invoke(plainText) ?? "cipher";
    public string AsymmetricDecrypt(string cipherTextBase64) => AsymmetricDecryptFunc?.Invoke(cipherTextBase64) ?? "plain";
    public string Sign(string plainText) => SignFunc?.Invoke(plainText) ?? "sig";
    public bool VerifySignature(string plainText, string signatureBase64) => VerifySignatureFunc?.Invoke(plainText, signatureBase64) ?? true;
    public string Hash(string plainText) => HashFunc?.Invoke(plainText) ?? "hash";
    public bool VerifyHash(string plainText, string hashValue) => VerifyHashFunc?.Invoke(plainText, hashValue) ?? true;
}

internal sealed class WebApiFixedCurrentUserService(CurrentUser currentUser) : ICurrentUserService
{
    public CurrentUser CurrentUser { get; } = currentUser;
}
