using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.Common.Services;
using Template.WebApi.Models.Auth;
// 啟用登入後回傳選單樹時取消註解：
// using Template.BusinessRule.MenuTreeService.Services;

namespace Template.WebApi.Controllers;

/// <summary>
/// 認證控制器，提供登入與登出。
/// </summary>
/// <remarks>
/// 建立認證控制器。
/// </remarks>
public class AuthController(
    ILogger<AuthController> logger,
    ILoginService loginService,
    ICurrentUserService currentUserService
    // 啟用登入後回傳選單樹時取消註解，並確認已註冊 IMenuTreeService：
    // , IMenuTreeService menuTreeService
    ) : BaseController<AuthController>(logger)
{
    private readonly ILoginService _loginService = loginService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    // 啟用登入後回傳選單樹時取消註解：
    // private readonly IMenuTreeService _menuTreeService = menuTreeService;

    /// <summary>
    /// 使用帳號密碼登入，成功後回傳 JWT Token。
    /// </summary>
    /// <param name="request">登入請求。</param>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var result = await _loginService.LoginAsync(request.UserId, request.Password, ip);

        if (!result.Success)
        {
            if (result.AccountDisabled)
                return StatusCode(StatusCodes.Status403Forbidden, result.ErrorMessage);

            return Unauthorized(result.ErrorMessage);
        }

        // 預設只回傳 JWT Token。
        // 若專案啟用 MenuTreeService，且需要登入後一併回傳選單樹，可取消註解下列區塊。
        // 注意：前端路由與元件對應由前端自行維護，後端只回傳選單節點、階層、排序與啟用狀態。
        //
        // var menuTree = await _menuTreeService.GetTreeAsync(isEnable: true);
        // return Ok(new
        // {
        //     Token = result.Token,
        //     MenuTree = menuTree
        // });

        return Ok(new { Token = result.Token });
    }

    /// <summary>
    /// 登出並撤銷目前 JWT Token。
    /// </summary>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader)
            || !authHeader.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return BadRequest("缺少 Bearer Token，無法執行登出。");

        var currentUser = _currentUserService.CurrentUser;
        if (string.IsNullOrWhiteSpace(currentUser.TokenId) || currentUser.ExpiredTime <= 0)
            return BadRequest("目前 Token 無法執行登出。");

        await _loginService.LogoutAsync(currentUser.TokenId, currentUser.ExpiredTime);
        return Ok(new { Message = "登出成功。" });
    }
}
