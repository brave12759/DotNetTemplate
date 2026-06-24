using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Template.BusinessRule.FunctionPermissionService.Enums;
using Template.BusinessRule.FunctionPermissionService.Models;
using Template.BusinessRule.FunctionPermissionService.Services;
using Template.Common.Extensions;

namespace Template.WebApi.Controllers;

/// <summary>
/// 功能操作權限維護 API。
/// </summary>
public class FunctionPermissionController(
    ILogger<FunctionPermissionController> logger,
    IFunctionPermissionService functionPermissionService) : AuthenticationController<FunctionPermissionController>(logger)
{
    private readonly IFunctionPermissionService _functionPermissionService = functionPermissionService;

    /// <summary>
    /// 取得功能操作權限平面清單。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? keyword, [FromQuery] bool? isEnable)
    {
        var permissions = await _functionPermissionService.GetListAsync(keyword, isEnable);
        return Ok(permissions);
    }

    /// <summary>
    /// 取得功能操作權限父子階層樹。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Tree([FromQuery] bool? isEnable)
    {
        var permissions = await _functionPermissionService.GetTreeAsync(isEnable);
        return Ok(permissions);
    }

    /// <summary>
    /// 依 FunctionPermissionId 取得單筆功能操作權限。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById([FromRoute] int functionPermissionId)
    {
        try
        {
            var permission = await _functionPermissionService.GetByIdAsync(functionPermissionId);
            if (permission is null)
                return NotFound(Message(FunctionPermissionMessageEnum.FunctionPermissionNotFound));

            return Ok(permission);
        }
        catch (ArgumentException)
        {
            return BadRequest(Message(FunctionPermissionMessageEnum.InvalidFunctionPermissionId));
        }
    }

    /// <summary>
    /// 新增功能操作權限。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FunctionPermissionCreateRequest request)
    {
        try
        {
            var permission = await _functionPermissionService.CreateAsync(request);
            return Ok(permission);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新功能操作權限。
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] FunctionPermissionUpdateRequest request)
    {
        try
        {
            var updated = await _functionPermissionService.UpdateAsync(request);
            if (!updated)
                return NotFound(Message(FunctionPermissionMessageEnum.FunctionPermissionNotFound));

            return Ok(new { Message = Message(FunctionPermissionMessageEnum.UpdatedSuccessfully) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 局部更新功能操作權限。
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> Patch([FromRoute] int functionPermissionId, [FromBody] JsonPatchDocument<FunctionPermissionUpdateRequest> patch)
    {
        try
        {
            var permission = await _functionPermissionService.GetByIdAsync(functionPermissionId);
            if (permission is null)
                return NotFound(Message(FunctionPermissionMessageEnum.FunctionPermissionNotFound));

            var request = new FunctionPermissionUpdateRequest
            {
                FunctionPermissionId = permission.FunctionPermissionId,
                ParentFunctionPermissionId = permission.ParentFunctionPermissionId,
                FunctionCode = permission.FunctionCode,
                FunctionName = permission.FunctionName,
                OperationCode = permission.OperationCode,
                SortOrder = permission.SortOrder,
                IsEnable = permission.IsEnable
            };

            patch.ApplyTo(request, error => ModelState.AddModelError(error.Operation.path, error.ErrorMessage));
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            request.FunctionPermissionId = functionPermissionId;
            var updated = await _functionPermissionService.UpdateAsync(request);
            if (!updated)
                return NotFound(Message(FunctionPermissionMessageEnum.FunctionPermissionNotFound));

            return Ok(new { Message = Message(FunctionPermissionMessageEnum.UpdatedSuccessfully) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 刪除功能操作權限；會同步刪除所有子孫權限與角色群組對應資料。
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] int functionPermissionId)
    {
        try
        {
            var deleted = await _functionPermissionService.DeleteAsync(functionPermissionId);
            if (!deleted)
                return NotFound(Message(FunctionPermissionMessageEnum.FunctionPermissionNotFound));

            return Ok(new { Message = Message(FunctionPermissionMessageEnum.DeletedSuccessfully) });
        }
        catch (ArgumentException)
        {
            return BadRequest(Message(FunctionPermissionMessageEnum.InvalidFunctionPermissionId));
        }
    }

    /// <summary>
    /// 依目前選單樹一鍵補足功能操作權限資料。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SyncFromMenuTree([FromQuery] bool includeDisabledMenus = false)
    {
        var result = await _functionPermissionService.SyncFromMenuTreeAsync(includeDisabledMenus);
        return Ok(result);
    }

    /// <summary>
    /// 取得指定角色群組已指派的功能操作權限平面清單。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRoleGroupPermissions([FromRoute] int roleGroupId, [FromQuery] bool? isEnable)
    {
        try
        {
            var permissions = await _functionPermissionService.GetRoleGroupPermissionsAsync(roleGroupId, isEnable);
            return Ok(permissions);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 取得指定角色群組已指派的功能操作權限樹。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> RoleGroupTree([FromRoute] int roleGroupId, [FromQuery] bool? isEnable)
    {
        try
        {
            var permissions = await _functionPermissionService.GetRoleGroupPermissionTreeAsync(roleGroupId, isEnable);
            return Ok(permissions);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 以整批覆蓋方式更新指定角色群組的功能操作權限。
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateRoleGroup([FromBody] RoleGroupFunctionPermissionUpdateRequest request)
    {
        try
        {
            var updated = await _functionPermissionService.UpdateRoleGroupPermissionsAsync(request);
            if (!updated)
                return NotFound(Message(FunctionPermissionMessageEnum.RoleGroupNotFound));

            return Ok(new { Message = Message(FunctionPermissionMessageEnum.UpdatedSuccessfully) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 取得指定使用者經由角色群組取得的功能操作權限樹。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> UserTree([FromRoute] string userId, [FromQuery] bool? isEnable)
    {
        try
        {
            var permissions = await _functionPermissionService.GetUserPermissionTreeAsync(userId, isEnable);
            return Ok(permissions);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 取得功能權限訊息列舉的描述文字。
    /// </summary>
    private static string Message(FunctionPermissionMessageEnum message) => message.GetDescription();
}
