using Microsoft.AspNetCore.Mvc;
using Template.BusinessRule.MenuTreeService.Models;
using Template.BusinessRule.MenuTreeService.Services;

namespace Template.WebApi.Controllers;

/// <summary>
/// 選單樹管理 API。
/// </summary>
public class MenuTreeController(
    ILogger<MenuTreeController> logger,
    IMenuTreeService menuTreeService) : AuthenticationController<MenuTreeController>(logger)
{
    private readonly IMenuTreeService _menuTreeService = menuTreeService;

    /// <summary>
    /// 取得選單平面清單。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? keyword, [FromQuery] bool? isEnable)
    {
        var menus = await _menuTreeService.GetListAsync(keyword, isEnable);
        return Ok(menus);
    }

    /// <summary>
    /// 取得父子階層選單樹。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Tree([FromQuery] bool? isEnable)
    {
        var menus = await _menuTreeService.GetTreeAsync(isEnable);
        return Ok(menus);
    }

    /// <summary>
    /// 依主鍵取得選單。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        try
        {
            var menu = await _menuTreeService.GetByIdAsync(id);
            if (menu is null)
                return NotFound("查無選單資料。");

            return Ok(menu);
        }
        catch (ArgumentException)
        {
            return BadRequest("請輸入有效的選單 ID。");
        }
    }

    /// <summary>
    /// 新增選單。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] MenuTreeCreateRequest request)
    {
        try
        {
            var menu = await _menuTreeService.CreateAsync(request);
            return Ok(menu);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新選單。
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] MenuTreeUpdateRequest request)
    {
        try
        {
            var updated = await _menuTreeService.UpdateAsync(request);
            if (!updated)
                return NotFound("查無選單資料。");

            return Ok(new { Message = "更新成功。" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 刪除選單。
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        try
        {
            var deleted = await _menuTreeService.DeleteAsync(id);
            if (!deleted)
                return NotFound("查無選單資料。");

            return Ok(new { Message = "刪除成功。" });
        }
        catch (ArgumentException)
        {
            return BadRequest("請輸入有效的選單 ID。");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
