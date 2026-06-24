using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Template.BusinessRule.RoleGroupService.Enums;
using Template.BusinessRule.RoleGroupService.Models;
using Template.BusinessRule.RoleGroupService.Services;
using Template.Common.Extensions;

namespace Template.WebApi.Controllers;

/// <summary>
/// 角色群組維護 API。
/// </summary>
public class RoleGroupController(
    ILogger<RoleGroupController> logger,
    IRoleGroupService roleGroupService) : AuthenticationController<RoleGroupController>(logger)
{
    private readonly IRoleGroupService _roleGroupService = roleGroupService;

    /// <summary>
    /// 取得角色群組平面清單。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? keyword, [FromQuery] bool? isEnable)
    {
        var roleGroups = await _roleGroupService.GetListAsync(keyword, isEnable);
        return Ok(roleGroups);
    }

    /// <summary>
    /// 取得角色群組父子階層樹。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Tree([FromQuery] bool? isEnable)
    {
        var roleGroups = await _roleGroupService.GetTreeAsync(isEnable);
        return Ok(roleGroups);
    }

    /// <summary>
    /// 依 RoleGroupId 取得單筆角色群組。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById([FromRoute] int roleGroupId)
    {
        try
        {
            var roleGroup = await _roleGroupService.GetByIdAsync(roleGroupId);
            if (roleGroup is null)
                return NotFound(Message(RoleGroupMessageEnum.RoleGroupNotFound));

            return Ok(roleGroup);
        }
        catch (ArgumentException)
        {
            return BadRequest(Message(RoleGroupMessageEnum.InvalidRoleGroupId));
        }
    }

    /// <summary>
    /// 新增角色群組。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RoleGroupCreateRequest request)
    {
        try
        {
            var roleGroup = await _roleGroupService.CreateAsync(request);
            return Ok(roleGroup);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新角色群組。
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] RoleGroupUpdateRequest request)
    {
        try
        {
            var updated = await _roleGroupService.UpdateAsync(request);
            if (!updated)
                return NotFound(Message(RoleGroupMessageEnum.RoleGroupNotFound));

            return Ok(new { Message = Message(RoleGroupMessageEnum.UpdatedSuccessfully) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 局部更新角色群組。
    /// </summary>
    [HttpPatch]
    public async Task<IActionResult> Patch([FromRoute] int roleGroupId, [FromBody] JsonPatchDocument<RoleGroupUpdateRequest> patch)
    {
        try
        {
            var roleGroup = await _roleGroupService.GetByIdAsync(roleGroupId);
            if (roleGroup is null)
                return NotFound(Message(RoleGroupMessageEnum.RoleGroupNotFound));

            var request = new RoleGroupUpdateRequest
            {
                RoleGroupId = roleGroup.RoleGroupId,
                ParentRoleGroupId = roleGroup.ParentRoleGroupId,
                RoleGroupName = roleGroup.RoleGroupName,
                Description = roleGroup.Description,
                SortOrder = roleGroup.SortOrder,
                IsEnable = roleGroup.IsEnable
            };

            patch.ApplyTo(request, error => ModelState.AddModelError(error.Operation.path, error.ErrorMessage));
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            request.RoleGroupId = roleGroupId;
            var updated = await _roleGroupService.UpdateAsync(request);
            if (!updated)
                return NotFound(Message(RoleGroupMessageEnum.RoleGroupNotFound));

            return Ok(new { Message = Message(RoleGroupMessageEnum.UpdatedSuccessfully) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 刪除角色群組；會同步刪除所有子孫角色群組與使用者對應資料。
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] int roleGroupId)
    {
        try
        {
            var deleted = await _roleGroupService.DeleteAsync(roleGroupId);
            if (!deleted)
                return NotFound(Message(RoleGroupMessageEnum.RoleGroupNotFound));

            return Ok(new { Message = Message(RoleGroupMessageEnum.DeletedSuccessfully) });
        }
        catch (ArgumentException)
        {
            return BadRequest(Message(RoleGroupMessageEnum.InvalidRoleGroupId));
        }
    }

    /// <summary>
    /// 取得指定使用者擁有的角色群組。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetUserRoleGroups([FromRoute] string userId, [FromQuery] bool? isEnable)
    {
        try
        {
            var roleGroups = await _roleGroupService.GetUserRoleGroupsAsync(userId, isEnable);
            return Ok(roleGroups);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 以整批覆蓋方式更新指定使用者擁有的角色群組。
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateUser([FromBody] UserRoleGroupUpdateRequest request)
    {
        try
        {
            var updated = await _roleGroupService.UpdateUserRoleGroupsAsync(request);
            if (!updated)
                return NotFound(Message(RoleGroupMessageEnum.UserNotFound));

            return Ok(new { Message = Message(RoleGroupMessageEnum.UpdatedSuccessfully) });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 取得角色群組訊息列舉的描述文字。
    /// </summary>
    private static string Message(RoleGroupMessageEnum message) => message.GetDescription();
}
