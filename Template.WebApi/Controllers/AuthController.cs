using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Template.BusinessRule.LoginService.Services;
using Template.Common.Services;
using Template.WebApi.Models.Auth;

namespace Template.WebApi.Controllers;

/// <summary>
/// 驗證與登入狀態 API。
/// </summary>
public class AuthController(
    ILogger<AuthController> logger,
    ILoginService loginService,
    ICurrentUserService currentUserService) : BaseController<AuthController>(logger)
{
    private readonly ILoginService _loginService = loginService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    /// <summary>
    /// 使用者以帳號密碼登入並取得 JWT Token。
    /// </summary>
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var result = await _loginService.LoginAsync(request.UserId, request.Password, ip);

        if (!result.Success)
            return ToLoginFailure(result);

        return Ok(new AuthTokenResponse { Token = result.Token });
    }

    /// <summary>
    /// 使用目前仍有效的 JWT Token 換取新的 JWT Token，讓前端可在 Token 到期前維持登入狀態。
    /// </summary>
    /// <remarks>
    /// 此 API 需要在舊 Token 尚未過期前呼叫。刷新成功後，舊 Token 會被撤銷，前端應改用回傳的新 Token。
    /// </remarks>
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Refresh()
    {
        var currentUser = _currentUserService.CurrentUser;
        if (string.IsNullOrWhiteSpace(currentUser.UserId) ||
            string.IsNullOrWhiteSpace(currentUser.TokenId) ||
            currentUser.ExpiredTime <= 0)
            return BadRequest("Token 資訊不完整，無法刷新。");

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty;
        var result = await _loginService.RefreshAsync(
            currentUser.UserId,
            currentUser.TokenId,
            currentUser.ExpiredTime,
            ip);

        if (!result.Success)
            return ToLoginFailure(result);

        return Ok(new AuthTokenResponse { Token = result.Token });
    }

    /// <summary>
    /// 取得目前 Token 對應的登入者資訊，供前端重整頁面後還原登入狀態。
    /// </summary>
    [Authorize]
    [HttpGet]
    public IActionResult Me()
    {
        return Ok(_currentUserService.CurrentUser);
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
            return BadRequest("缺少 Bearer Token。");

        var currentUser = _currentUserService.CurrentUser;
        if (string.IsNullOrWhiteSpace(currentUser.TokenId) || currentUser.ExpiredTime <= 0)
            return BadRequest("Token 資訊不完整，無法登出。");

        await _loginService.LogoutAsync(currentUser.TokenId, currentUser.ExpiredTime);
        return Ok(new { Message = "登出成功。" });
    }

    /// <summary>
    /// 將登入或刷新失敗結果轉成一致的 HTTP 回應。
    /// </summary>
    private IActionResult ToLoginFailure(Template.Common.Models.LoginResult result)
    {
        if (result.AccountDisabled)
            return StatusCode(StatusCodes.Status403Forbidden, result.ErrorMessage);

        if (result.PasswordExpired)
            return StatusCode(StatusCodes.Status403Forbidden, result.ErrorMessage);

        return Unauthorized(result.ErrorMessage);
    }
}
