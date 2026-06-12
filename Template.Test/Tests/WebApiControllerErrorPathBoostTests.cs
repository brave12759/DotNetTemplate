using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.CryptographyService.Models;
using Template.BusinessRule.DepartmentService.Models;
using Template.BusinessRule.FunctionPermissionService.Models;
using Template.BusinessRule.MenuTreeService.Models;
using Template.BusinessRule.RoleGroupService.Models;
using Template.Common.Models.User;
using Template.WebApi.Controllers;

namespace Template.Test.Tests;

[TestClass]
public class WebApiControllerErrorPathBoostTests
{
    [TestMethod]
    public async Task Department_ErrorBranches_Should_ReturnBadRequestOrNotFound()
    {
        var service = new FakeDepartmentService
        {
            CreateAsyncFunc = _ => throw new ArgumentException("bad"),
            UpdateAsyncFunc = _ => throw new ArgumentException("bad"),
            GetByIdAsyncFunc = _ => Task.FromResult<Template.BusinessRule.DepartmentService.Models.DepartmentDto?>(null),
            DeleteAsyncFunc = _ => throw new ArgumentException("bad")
        };
        var controller = new DepartmentController(NullLogger<DepartmentController>.Instance, service);

        Assert.IsInstanceOfType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Create(new DepartmentCreateRequest()));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Update(new DepartmentUpdateRequest()));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Delete(1));
    }

    [TestMethod]
    public async Task User_ErrorBranches_Should_ReturnBadRequestOrNotFound()
    {
        var service = new FakeUserService
        {
            GetListAsyncFunc = (_, _, _, _, _, _, _) => throw new ArgumentException("bad"),
            GetByIdAsyncFunc = _ => throw new ArgumentException("bad"),
            UpdateAsyncFunc = _ => throw new ArgumentException("bad"),
            DeleteAsyncFunc = _ => throw new ArgumentException("bad"),
            ResetPasswordAsyncFunc = _ => throw new ArgumentException("bad"),
            ChangePasswordAsyncFunc = _ => throw new ArgumentException("bad")
        };
        var controller = new UserController(NullLogger<UserController>.Instance, service);

        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.List(null, null, null));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.GetById(1));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Update(new UserUpdateRequest()));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Delete(1));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.ResetPassword(new UserResetPasswordRequest()));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.ChangePassword(new UserChangePasswordRequest()));

        service.ChangePasswordAsyncFunc = _ => Task.FromResult(false);
        Assert.IsInstanceOfType<NotFoundObjectResult>(await controller.ChangePassword(new UserChangePasswordRequest()));
    }

    [TestMethod]
    public async Task FunctionPermission_ErrorBranches_Should_ReturnBadRequestOrNotFound()
    {
        var service = new FakeFunctionPermissionService
        {
            GetByIdAsyncFunc = _ => Task.FromResult<FunctionPermissionDto?>(null),
            CreateAsyncFunc = _ => throw new ArgumentException("bad"),
            UpdateAsyncFunc = _ => throw new ArgumentException("bad"),
            DeleteAsyncFunc = _ => throw new ArgumentException("bad"),
            GetRoleGroupPermissionTreeAsyncFunc = (_, _) => throw new ArgumentException("bad"),
            GetUserPermissionTreeAsyncFunc = (_, _) => throw new ArgumentException("bad"),
            UpdateRoleGroupPermissionsAsyncFunc = _ => throw new ArgumentException("bad")
        };
        var controller = new FunctionPermissionController(NullLogger<FunctionPermissionController>.Instance, service);

        Assert.IsInstanceOfType<NotFoundObjectResult>(await controller.GetById(1));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Create(new FunctionPermissionCreateRequest()));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Update(new FunctionPermissionUpdateRequest()));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Delete(1));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.RoleGroupTree(1, true));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.UserTree("u1", true));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.UpdateRoleGroup(new RoleGroupFunctionPermissionUpdateRequest()));
    }

    [TestMethod]
    public async Task MenuTree_ErrorBranches_Should_ReturnBadRequestOrNotFound()
    {
        var service = new FakeMenuTreeService
        {
            GetByIdAsyncFunc = _ => throw new ArgumentException("bad"),
            CreateAsyncFunc = _ => throw new ArgumentException("bad"),
            UpdateAsyncFunc = _ => throw new ArgumentException("bad"),
            DeleteAsyncFunc = _ => Task.FromResult(false)
        };
        var controller = new MenuTreeController(NullLogger<MenuTreeController>.Instance, service);

        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.GetById(1));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Create(new MenuTreeCreateRequest()));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Update(new MenuTreeUpdateRequest()));
        Assert.IsInstanceOfType<NotFoundObjectResult>(await controller.Delete(1));
    }

    [TestMethod]
    public async Task RoleGroup_ErrorBranches_Should_ReturnBadRequestOrNotFound()
    {
        var service = new FakeRoleGroupService
        {
            GetByIdAsyncFunc = _ => throw new ArgumentException("bad"),
            CreateAsyncFunc = _ => throw new ArgumentException("bad"),
            UpdateAsyncFunc = _ => throw new ArgumentException("bad"),
            DeleteAsyncFunc = _ => throw new ArgumentException("bad"),
            GetUserRoleGroupsAsyncFunc = (_, _) => throw new ArgumentException("bad"),
            UpdateUserRoleGroupsAsyncFunc = _ => throw new ArgumentException("bad")
        };
        var controller = new RoleGroupController(NullLogger<RoleGroupController>.Instance, service);

        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.GetById(1));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Create(new RoleGroupCreateRequest()));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Update(new RoleGroupUpdateRequest()));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.Delete(1));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.GetUserRoleGroups("u1", true));
        Assert.IsInstanceOfType<BadRequestObjectResult>(await controller.UpdateUser(new UserRoleGroupUpdateRequest { UserId = "u1" }));

        service.GetByIdAsyncFunc = _ => Task.FromResult<RoleGroupDto?>(null);
        Assert.IsInstanceOfType<NotFoundObjectResult>(await controller.GetById(1));
    }

    [TestMethod]
    public void Cryptography_RemainingBranches_Should_ReturnExpected()
    {
        var service = new FakeCryptographyService
        {
            GenerateRsaKeyPairFunc = _ => throw new ArgumentException("bad"),
            SymmetricEncryptFunc = _ => "cipher",
            SymmetricDecryptFunc = _ => throw new ArgumentException("bad"),
            AsymmetricEncryptFunc = _ => "cipher2",
            AsymmetricDecryptFunc = _ => throw new ArgumentException("bad"),
            SignFunc = _ => "sig",
            VerifySignatureFunc = (_, _) => throw new ArgumentException("bad"),
            HashFunc = _ => "hash",
            VerifyHashFunc = (_, _) => throw new ArgumentException("bad")
        };
        var controller = new CryptographyController(NullLogger<CryptographyController>.Instance, service);

        Assert.IsInstanceOfType<OkObjectResult>(controller.SymmetricEncrypt(new CryptographyServiceSymmetricEncryptRequest { PlainText = "a" }));
        Assert.IsInstanceOfType<BadRequestObjectResult>(controller.SymmetricDecrypt(new CryptographyServiceSymmetricDecryptRequest { CipherTextBase64 = "x" }));
        Assert.IsInstanceOfType<BadRequestObjectResult>(controller.GenerateRsaKeyPair(new CryptographyServiceGenerateRsaKeyPairRequest { KeySizeBits = 1024 }));
        Assert.IsInstanceOfType<OkObjectResult>(controller.AsymmetricEncrypt(new CryptographyServiceAsymmetricEncryptRequest { PlainText = "a" }));
        Assert.IsInstanceOfType<BadRequestObjectResult>(controller.AsymmetricDecrypt(new CryptographyServiceAsymmetricDecryptRequest { CipherTextBase64 = "x" }));
        Assert.IsInstanceOfType<OkObjectResult>(controller.Sign(new CryptographyServiceSignatureRequest { PlainText = "a" }));
        Assert.IsInstanceOfType<BadRequestObjectResult>(controller.VerifySignature(new CryptographyServiceVerifySignatureRequest { PlainText = "a", SignatureBase64 = "b" }));
        Assert.IsInstanceOfType<OkObjectResult>(controller.Hash(new CryptographyServiceHashRequest { PlainText = "a" }));
        Assert.IsInstanceOfType<BadRequestObjectResult>(controller.VerifyHash(new CryptographyServiceVerifyHashRequest { PlainText = "a", HashValue = "h" }));
    }
}
