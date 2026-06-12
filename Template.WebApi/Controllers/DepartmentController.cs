using Microsoft.AspNetCore.Mvc;
using Template.BusinessRule.DepartmentService.Models;
using Template.BusinessRule.DepartmentService.Services;

namespace Template.WebApi.Controllers;

/// <summary>
/// 部門管理 API。
/// </summary>
public class DepartmentController(
    ILogger<DepartmentController> logger,
    IDepartmentService departmentService) : AuthenticationController<DepartmentController>(logger)
{
    private readonly IDepartmentService _departmentService = departmentService;

    /// <summary>
    /// 查詢部門清單，可依關鍵字與啟用狀態篩選。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? keyword, [FromQuery] bool? isEnable)
    {
        var departments = await _departmentService.GetListAsync(keyword, isEnable);
        return Ok(departments);
    }

    /// <summary>
    /// 查詢部門樹狀資料。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Tree([FromQuery] bool? isEnable)
    {
        var departments = await _departmentService.GetTreeAsync(isEnable);
        return Ok(departments);
    }

    /// <summary>
    /// 依部門 ID 查詢單筆部門。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById([FromQuery] int deptId)
    {
        try
        {
            var department = await _departmentService.GetByIdAsync(deptId);
            if (department is null)
                return NotFound("Department not found.");

            return Ok(department);
        }
        catch (ArgumentException)
        {
            return BadRequest("Invalid department id.");
        }
    }

    /// <summary>
    /// 建立部門。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepartmentCreateRequest request)
    {
        try
        {
            var department = await _departmentService.CreateAsync(request);
            return Ok(department);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 更新部門。
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] DepartmentUpdateRequest request)
    {
        try
        {
            var updated = await _departmentService.UpdateAsync(request);
            if (!updated)
                return NotFound("Department not found.");

            return Ok(new { Message = "Updated successfully." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 刪除部門；有子部門或使用者歸屬時不可刪除。
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] int deptId)
    {
        try
        {
            var deleted = await _departmentService.DeleteAsync(deptId);
            if (!deleted)
                return NotFound("Department not found.");

            return Ok(new { Message = "Deleted successfully." });
        }
        catch (ArgumentException)
        {
            return BadRequest("Invalid department id.");
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
