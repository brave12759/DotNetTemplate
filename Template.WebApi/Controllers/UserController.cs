using Microsoft.AspNetCore.Mvc;
using Template.BusinessRule.UserService.Services;
using Template.Common.Models.User;

namespace Template.WebApi.Controllers;

/// <summary>
/// 使用者管理控制器，提供 CRUD 與重設密碼。
/// </summary>
public class UserController(
    ILogger<UserController> logger,
    IUserService userService) : AuthenticationController<UserController>(logger)
{
    private readonly IUserService _userService = userService;

    /// <summary>
    /// 取得使用者清單。
    /// </summary>
    /// <param name="keyword">關鍵字，會比對帳號、姓名、Email、手機、部門 ID 與部門名稱。</param>
    /// <param name="isEnable">啟用狀態；空值代表不篩選。</param>
    /// <param name="deptId">部門 ID；空值代表不篩選部門。</param>
    /// <param name="includeSubDepartments">是否包含指定部門底下所有子部門。</param>
    /// <param name="enablePaging">是否啟用分頁。</param>
    /// <param name="page">頁碼（從 1 開始）。</param>
    /// <param name="pageSize">每頁筆數。</param>
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? keyword,
        [FromQuery] bool? isEnable,
        [FromQuery] int? deptId,
        [FromQuery] bool includeSubDepartments = false,
        [FromQuery] bool enablePaging = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var users = await _userService.GetListAsync(
                keyword,
                isEnable,
                deptId,
                includeSubDepartments,
                enablePaging,
                page,
                pageSize);
            return Ok(users);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// 依主鍵取得單一使用者。
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user is null)
                return NotFound("找不到指定的使用者。");

            return Ok(user);
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認使用者編號。");
        }
    }

    /// <summary>
    /// 建立使用者。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] UserCreateRequest request)
    {
        try
        {
            var user = await _userService.CreateAsync(request);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, ex.Message);
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認使用者資料。");
        }
    }

    /// <summary>
    /// 更新使用者基本資料（不含密碼）。
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UserUpdateRequest request)
    {
        try
        {
            var updated = await _userService.UpdateAsync(request);
            if (!updated)
                return NotFound("找不到指定的使用者。");

            return Ok(new { Message = "更新成功。" });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認更新資料。");
        }
    }

    /// <summary>
    /// 刪除使用者。
    /// </summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        try
        {
            var deleted = await _userService.DeleteAsync(id);
            if (!deleted)
                return NotFound("找不到指定的使用者。");

            return Ok(new { Message = "刪除成功。" });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認使用者編號。");
        }
    }

    /// <summary>
    /// 重設使用者密碼。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ResetPassword([FromBody] UserResetPasswordRequest request)
    {
        try
        {
            var reset = await _userService.ResetPasswordAsync(request);
            if (!reset)
                return NotFound("找不到指定的使用者。");

            return Ok(new { Message = "密碼重設成功。" });
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認重設密碼資料。");
        }
    }

    /// <summary>
    /// 修改使用者密碼（需驗證舊密碼）。
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordRequest request)
    {
        try
        {
            var changed = await _userService.ChangePasswordAsync(request);
            if (!changed)
                return NotFound("找不到指定的使用者。");

            return Ok(new { Message = "密碼修改成功。" });
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized("舊密碼驗證失敗，請確認後重試。");
        }
        catch (ArgumentException)
        {
            return BadRequest("輸入參數有誤，請確認密碼資料。");
        }
    }
}
